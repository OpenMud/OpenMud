using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.PrebuildProcessor;
using OpenMud.Mudpiler.Compiler.Core.Visitor;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.TypeSolver;

namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building;

public class CSharpModule : IDreamMakerSymbolResolver
{
    private static readonly long MAX_METHOD_REDECLARATIONS = 1000000000000;

    public static readonly string ROOT_NAMESPACE = "DmlGenerate";

    private static readonly ExpressionVisitor EXPR = new();

    private readonly Dictionary<string, ClassDeclarationSyntax> classDeclarations = new();

    private readonly IClassPrebuildProcessor[] classPreBuildProcessors =
    {
        new ContexFieldTranslator()
    };

    private readonly Dictionary<string, FieldDeclarationSyntax> fieldDeclarations = new();
    private readonly Dictionary<string, Dictionary<string, ExpressionSyntax>> fieldInitializers = new();
    
    private readonly Dictionary<ModuleMethodDeclarationKey, ClassDeclarationSyntax> methodClassArgumentBuilderDeclarations = new();

    private readonly Dictionary<ModuleMethodDeclarationKey, ClassDeclarationSyntax> methodClassDeclarations = new();
    private readonly Dictionary<ModuleMethodDeclarationKey, MethodDeclarationSyntax> methodDeclarations = new();

    private readonly Dictionary<ModuleMethodDeclarationKey, ClassDeclarationSyntax>
        methodExecutionContextClassDeclarations = new();

    private readonly Dictionary<string, string> primitiveClassNames = new();
    private readonly Dictionary<string, Dictionary<string, AttributeSyntax>> settings = new();

    private readonly CSharpSyntaxRewriter[] Rewriters;

    public CSharpModule(bool generateDebuggableBuild)
    {
        foreach (var t in DmlPath.DefaultCompilerImplementationTypes)
            primitiveClassNames[t] = DefineClass(t, c => c).Identifier.Text;

        var rewriteConfiguration = new CSharpSyntaxRewriter[]
        {
            new IsTypeResolverRewriter(LookUpClass, LookUpClassName),
            new ClassNamespaceResolverWalker(),
            new InheritanceGraphRewriter(LookUpClass, p => primitiveClassNames[DmlPath.BuildQualifiedDeclarationName(p)],
                LookUpClassName),
            new GlobalVariableRewriter(LookUpClass, LookUpClassName),
            new BaseConstructorCallChainResolverWalker(),
            new ImplicitReturnRewriter(),
            new LiteralObjRefRewriter(),
            new SelfSuperCallRewriter(LookUpClass, LookUpClassName),
            new ManagedArgListWrapper(),
            new GenerateLocalContextRewriter(),
            new AsyncSegmentorRewriter(),
            new ImmediateEvaluationRewriter(),
            //new SourceMapperRewriter(),
            new ReservedKeywordVariableNameConflictResolver(),
            new CilReturnStatementRewriter()
        }.AsEnumerable();

        if (generateDebuggableBuild)
            rewriteConfiguration = rewriteConfiguration.Append(new DebuggableProcRewriter());

        Rewriters = rewriteConfiguration.ToArray();
    }

    public IEnumerable<ModuleMethodDeclarationKey> MethodDeclarations => methodDeclarations.Keys.ToList();

    public void DefineClassMethod(string fullPath, Func<MethodDeclarationSyntax, MethodDeclarationSyntax> decl,
        int declarationOrder)
    {
        if (fullPath == null || fullPath.Length == 0)
            throw new Exception("Method name cannot be blank or null...");

        var modifiers = DmlPath.ParseNamespacePath(fullPath, out var effectiveClassNameFullPath, out var methodName, true);

        var effectiveFullPath = DmlPath.BuildQualifiedDeclarationName(effectiveClassNameFullPath);
        var fullMethodName = DmlPath.BuildQualifiedDeclarationName(effectiveFullPath, methodName!);

        if (methodName.Length == 0)
            throw new ArgumentException("Invalid method name.");

        var defaultModifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        var defaultAtrtibutes = SyntaxFactory.AttributeList();

        foreach (var m in modifiers)
            switch (m)
            {
                case DmlPathModifier.Static:
                    defaultModifiers = defaultModifiers.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                    break;
                case DmlPathModifier.Proc:
                    continue;
                case DmlPathModifier.Verb:
                    defaultAtrtibutes = defaultAtrtibutes.AddAttributes(
                        SettingsBuilder.CreateVerb()
                    );
                    break;
                default:
                    throw new Exception("Unsupported modifier on method: " + m);
            }

        var declarationKey = new ModuleMethodDeclarationKey(declarationOrder, fullMethodName);

        if (!methodDeclarations.ContainsKey(declarationKey))
        {
            var defaultImpl = SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(SyntaxKind.VoidKeyword)
                    ),
                    methodName
                )
                .WithModifiers(defaultModifiers);

            if (defaultAtrtibutes.Attributes.Any())
                defaultImpl = defaultImpl.AddAttributeLists(defaultAtrtibutes);

            methodDeclarations[declarationKey] = defaultImpl;
        }

        methodDeclarations[declarationKey] = decl(methodDeclarations[declarationKey]);

        var baseClass = TouchAndGet(effectiveClassNameFullPath);

        var methodClassName = baseClass.Identifier.Text + "_" + methodName + "_" + declarationOrder;
        var argBuilderClassName = methodClassName + "_arginit";

        methodClassArgumentBuilderDeclarations[declarationKey] = DmlMethodBuilder.BuildDefaultArgListBuilderMethodClass(
            argBuilderClassName,
            effectiveClassNameFullPath,
            methodDeclarations[declarationKey]
        );
        
        methodExecutionContextClassDeclarations[declarationKey] =
            DmlMethodBuilder.BuildMethodExecutionContextClass(methodDeclarations[declarationKey], effectiveClassNameFullPath,
                methodClassName);

        methodClassDeclarations[declarationKey] = DmlMethodBuilder.BuildMethodClass(
            effectiveClassNameFullPath,
            methodClassName,
            declarationOrder,
            methodDeclarations[declarationKey],
            methodExecutionContextClassDeclarations[declarationKey],
            methodClassArgumentBuilderDeclarations[declarationKey]
        );
    }

    public ClassDeclarationSyntax TouchAndGet(string path)
    {
        return DefineClass(path, c => c);
    }

    public void Touch(string path)
    {
        TouchAndGet(path);
    }

    public void DefineClassField(string fullPath, string typeHint, Func<FieldDeclarationSyntax, FieldDeclarationSyntax> decl)
    {
        if (fullPath == null || fullPath.Length == 0)
            throw new Exception("Field name cannot be blank or null...");

        var modifiers = DmlPath.ParseNamespacePath(fullPath, out var effectiveFullPath, out var fieldName, true);

        if (fieldName.Length == 0)
            throw new ArgumentException("Invalid field name.");

        //Define all of the base classes..
        var baseClassName = DmlPath.BuildQualifiedDeclarationName(effectiveFullPath);

        var defaultModifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));

        foreach (var m in modifiers)
            switch (m)
            {
                case DmlPathModifier.Global:
                case DmlPathModifier.Static:
                    defaultModifiers = defaultModifiers.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                    break;
                case DmlPathModifier.Tmp:
                    break;
                default:
                    throw new Exception("Unsupported modifier on Field: " + m);
            }

        var absoluteName = DmlPath.BuildQualifiedDeclarationName(baseClassName, fieldName);

        if (!fieldDeclarations.ContainsKey(absoluteName))
        {
            var defaultImpl = SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.ParseTypeName("dynamic"),
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.VariableDeclarator(fieldName)
                            .WithAdditionalAnnotations(BuilderAnnotations.CreateTypeHints(typeHint))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(EXPR.CreateVariable()))
                    })
                )
            ).WithModifiers(defaultModifiers);

            fieldDeclarations[absoluteName] = defaultImpl;
            DefineClass(baseClassName, c => c.AddMembers(defaultImpl));
        }

        CreateOrReplaceMember(baseClassName, fieldName, decl);
    }

    private AttributeSyntax? CollectSetting(ModuleMethodDeclarationKey k, string keyName)
    {
        if (!settings.TryGetValue(k.Name, out var s) || !s.ContainsKey(keyName))
            return null;

        var originDefs = methodClassDeclarations
            .Where(x => x.Key.Name == k.Name)
            .OrderByDescending(x => x.Key.DeclarationOrder)
            .Select(k => k.Value.AttributeLists)
            .SelectMany(x => x.Select(y => y.Attributes))
            .SelectMany(x => x)
            .Where(x => SettingsBuilder.IsAttribute(keyName, x.Name))
            .ToList();

        return originDefs.FirstOrDefault();
    }

    public void DefineMethodConfiguration(string key, Func<AttributeSyntax, AttributeSyntax> decl, int declarationOrder,
        bool replaceExisting = true)
    {
        if (key == null || key.Length == 0)
            throw new Exception("Field name cannot be blank or null...");

        var components = key.Split("/").Where(x => x.Length > 0).ToList();

        if (components.Count < 2)
            throw new Exception("Invalid configuration path...");

        var keyName = components.Last();
        var hostPath = DmlPath.BuildQualifiedDeclarationName(DmlPath.BuildPath(components.Take(components.Count - 1)));

        if (!settings.ContainsKey(hostPath))
            settings[hostPath] = new Dictionary<string, AttributeSyntax>();

        var declarationKey = new ModuleMethodDeclarationKey(declarationOrder, hostPath);
        var currentSetting = CollectSetting(declarationKey, keyName);

        if (currentSetting == null)
        {
            currentSetting = SettingsBuilder.CreateAttribute(keyName);
            DefineClassMethod(
                hostPath,
                m =>
                    m.AddAttributeLists(
                        SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(new[]
                            {
                                currentSetting
                            })
                        )
                    ),
                declarationOrder
            );
        }
        else if (!replaceExisting)
        {
            return;
        }

        var newImpl = decl(currentSetting);

        DefineClassMethod(
            hostPath,
            m =>
            {
                var existing = m.AttributeLists.SelectMany(x => x.Attributes)
                    .Where(x => SettingsBuilder.IsAttribute(keyName, x.Name)).Single();
                settings[hostPath][keyName] = newImpl;
                return m.ReplaceNode(existing, newImpl);
            },
            declarationOrder
        );
    }

    public void DefineFieldInitializer(string fullPath, ExpressionSyntax initializer, bool replaceExisting = true)
    {
        DmlPath.ParseNamespacePath(fullPath, out var parent, out var fieldName, true);

        //Make sure the class is initialized.
        DefineClass(parent, c => c);

        if (!fieldInitializers.ContainsKey(parent))
            fieldInitializers[parent] = new Dictionary<string, ExpressionSyntax>();

        if (fieldInitializers[parent].ContainsKey(fieldName) && !replaceExisting)
            return;

        fieldInitializers[parent][fieldName] = initializer;
    }

    public ExpressionSyntax ResolveGlobal(string fullPath)
    {
        throw new NotImplementedException();
    }

    public TypeSyntax ResolvePathType(string fullPath)
    {
        throw new NotImplementedException();
    }

    public string DefineSupportMethod(string baseClass, Func<MethodDeclarationSyntax, MethodDeclarationSyntax> decl)
    {
        var parent = DmlPath.BuildQualifiedDeclarationName(baseClass);

        //Make sure the class is initialized.
        DefineClass(parent, c => c);

        var supportIdx = 0;
        Func<int, string> supportName = idx => DmlPath.BuildQualifiedDeclarationName(parent, $"__SUPPORT{idx}");

        var declKey = new ModuleMethodDeclarationKey(0, supportName(supportIdx));

        while (methodDeclarations.ContainsKey(declKey))
        {
            supportIdx++;
            declKey = new ModuleMethodDeclarationKey(0, supportName(supportIdx));
        }

        var selectedName = declKey.Name;

        DefineClassMethod(selectedName, decl, 0);

        return DmlPath.ExtractComponentName(selectedName);
    }

    private ClassDeclarationSyntax LookUpClass(string n)
    {
        return classDeclarations[DmlPath.BuildQualifiedDeclarationName(n)];
    }

    private string LookUpClassName(ClassDeclarationSyntax n)
    {
        return classDeclarations
            .Where(y => y.Value.Identifier.Text == n.Identifier.Text).Single().Key;
    }

    private ClassDeclarationSyntax DefineClass(string? fullPath, Func<ClassDeclarationSyntax, ClassDeclarationSyntax> define)
    {
        if (fullPath == null)
            fullPath = DmlPath.GLOBAL_PATH;

        //Define all of the base classes..
        var pathName = DmlPath.BuildQualifiedDeclarationName(fullPath);

        if (pathName.Length == 0)
            throw new ArgumentException("Invalid class name path.");

        var baseClassName = DmlPath.ResolveParentClass(pathName);
        if (baseClassName != null)
            DefineClass(baseClassName, c => c);

        if (!classDeclarations.TryGetValue(pathName, out var decl))
        {
            var clsName = DmlPath.ExtractComponentName(pathName) + $"_{classDeclarations.Count}";

            decl = SyntaxFactory.ClassDeclaration(
                    clsName
                )
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));

            var classNames = DmlPath.EnumerateTypeAliasesOf(pathName);

            decl = decl.WithAttributeLists(
                SyntaxFactory.List(new[]
                {
                    SyntaxFactory.AttributeList(
                        SyntaxFactory.SeparatedList(
                            classNames.Select(clsName =>
                                SyntaxFactory.Attribute(
                                    SyntaxFactory.ParseName(typeof(EntityDefinition).FullName),
                                    SyntaxFactory.AttributeArgumentList(
                                        SyntaxFactory.SeparatedList(new[]
                                        {
                                            SyntaxFactory.AttributeArgument(
                                                SyntaxFactory.LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    SyntaxFactory.Literal(clsName)
                                                )
                                            )
                                        })
                                    )
                                )
                            )
                        )
                    )
                })
            );
        }

        var cls = define(decl).WithAdditionalAnnotations(BuilderAnnotations.CreateClassPathAnnotation(pathName));
        classDeclarations[pathName] = cls;

        return cls;
    }

    private void CreateOrReplaceMember(string clsName, string memberName,
        Func<FieldDeclarationSyntax, FieldDeclarationSyntax> newMem)
    {
        var absoluteName = DmlPath.BuildQualifiedDeclarationName(clsName, memberName);

        var replacement = CreateOrReplaceMember<FieldDeclarationSyntax>(
            clsName,
            newMem,
            cls => cls.Members
                .Where(
                    x => x is FieldDeclarationSyntax y &&
                         y.Declaration.Variables.Single().Identifier.Text == memberName
                )
                .Cast<FieldDeclarationSyntax>()
                .Single()
        );

        var currentDeclarator = fieldDeclarations[absoluteName].DescendantNodes().Where(x =>
            x is VariableDeclaratorSyntax && BuilderAnnotations.ExtractTypeHintAnnotation(x, out var _)).Single();

        if (!BuilderAnnotations.ExtractTypeHintAnnotation(currentDeclarator, out var hint))
            throw new Exception("Type hint is missing.");

        replacement = replacement.ReplaceNodes(
            replacement.DescendantNodes().Where(x => x is VariableDeclaratorSyntax).Cast<VariableDeclaratorSyntax>(),
            (n, m) => n.WithAdditionalAnnotations(BuilderAnnotations.CreateTypeHints(hint))
        );

        fieldDeclarations[absoluteName] = replacement;
    }

    private T CreateOrReplaceMember<T>(string clsName, Func<T, T> newMem, Func<ClassDeclarationSyntax, T> locator)
        where T : SyntaxNode
    {
        var baseClass = DefineClass(clsName, c => c);

        var existingNode = locator(baseClass);

        var newMember = newMem(existingNode);

        baseClass = DefineClass(clsName, c => c);
        existingNode = locator(baseClass);

        DefineClass(clsName, c =>
        {
            return c.ReplaceNode(
                existingNode,
                newMember
            );
        });

        return newMember;
    }
    
    private BlockSyntax CreateFieldInitializers(string fullName)
    {
        var fieldInitializerStatements = new List<StatementSyntax>();

        fieldInitializerStatements.Add(SyntaxFactory.ExpressionStatement(ExpressionVisitor.CreateSuperCall()));
        
        if (fieldInitializers.TryGetValue(fullName, out var fields))
        {
            foreach (var f in fields)
            {
                fieldInitializerStatements.Add(
                    SyntaxFactory.ExpressionStatement(
                        EXPR.CreateAssignment(
                            Util.IdentifierName(f.Key),
                            f.Value
                        )
                    )
                );
            }
        }

        var result = SyntaxFactory.Block(fieldInitializerStatements.ToArray());

        return result;
    }

    private bool IsClassImmediateChildOf(string super, string child)
    {
        var childSuper = DmlPath.ResolveParentClass(child);

        if (childSuper == null)
            return false;

        return (DmlPath.IsGlobal(super) && DmlPath.IsGlobal(childSuper)) || childSuper == super;
    }

    private BlockSyntax CreateConstructor(string fullName)
    {
        var statements = new List<StatementSyntax>();

        var depth = DmlPath.ComputePathDepth(fullName);

        var childMethods = methodClassDeclarations.Where(x =>
            IsClassImmediateChildOf(fullName, x.Key.Name)
        ).OrderBy(m => m.Key.DeclarationOrder);

        foreach (var (m, cls) in childMethods)
        {
            if (m.DeclarationOrder >= MAX_METHOD_REDECLARATIONS ||
                depth * MAX_METHOD_REDECLARATIONS / MAX_METHOD_REDECLARATIONS != depth)
                throw new Exception("Inheritance or declaration depth too deep.");

            var methodDepth = depth * MAX_METHOD_REDECLARATIONS + m.DeclarationOrder;

            statements.Add(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ThisExpression()
                                .WithAdditionalAnnotations(BuilderAnnotations.DontWrapAnnotation),
                            SyntaxFactory.IdentifierName("RegisterProcedure")
                        ),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(new[]
                            {
                                SyntaxFactory.Argument(SyntaxFactory
                                    .LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(methodDepth))
                                    .WithAdditionalAnnotations(BuilderAnnotations.DontWrapAnnotation)),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(m.DeclarationOrder)
                                    )
                                ),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        SyntaxFactory.Literal(m.Name)
                                    )
                                )
                            })
                        )
                    )
                )
            );
        }

        return SyntaxFactory.Block(statements.ToArray());
    }

    private ClassDeclarationSyntax BuildClassConstructor(string name, ClassDeclarationSyntax builtClass)
    {
        return builtClass.AddMembers(
            SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.List<AttributeListSyntax>(),
                    SyntaxFactory.TokenList(new[]
                    {
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)
                    }),
                    SyntaxFactory.ParseTypeName("dynamic"),
                    null,
                    SyntaxFactory.Identifier("_constructor"),
                    null,
                    SyntaxFactory.ParameterList(),
                    SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                    CreateConstructor(name),
                    SyntaxFactory.Token(SyntaxKind.None)
                )
                .WithAdditionalAnnotations(BuilderAnnotations.DmlImmediateEvaluateMethod)
        );
    }

    private void BuildClassFieldInitializer(string name)
    {
        var initName = "_constructor_fieldinit";

        DefineClassMethod(DmlPath.BuildQualifiedDeclarationName(name, $"{initName}"), old => old.WithBody(CreateFieldInitializers(name)), 0);
    }

    private ClassDeclarationSyntax AnnotateFieldTypeHints(ClassDeclarationSyntax value)
    {
        var fieldDeclarations = value.Members.Where(m => m is FieldDeclarationSyntax fld).Cast<FieldDeclarationSyntax>();
        return value.ReplaceNodes(fieldDeclarations, (_, n) => {
            var typeHint = BuilderAnnotations.ExtractTypeHintAnnotationOrDefault(n);

            if (typeHint.Length == 0)
                return n;

            return n.WithAttributeLists(
                SyntaxFactory.List(
                    n.AttributeLists
                    .Append(SyntaxFactory.AttributeList(
                        SyntaxFactory.SeparatedList<AttributeSyntax>(new AttributeSyntax[] {
                            SyntaxFactory.Attribute(
                                SyntaxFactory.ParseName(typeof(RuntimeEnvironment.Settings.FieldTypeHint).FullName),
                                SyntaxFactory.AttributeArgumentList(
                                    SyntaxFactory.SeparatedList(new[]
                                    {
                                        SyntaxFactory.AttributeArgument(
                                            SyntaxFactory.LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                SyntaxFactory.Literal(typeHint)
                                            )
                                        )
                                    })
                                )
                            )
                        }
                    ))
                )
            ));
        });
    }

    public CompilationUnitSyntax CreateCompilationUnit()
    {
        foreach (var u in fieldDeclarations.Keys.ToList())
            DefineFieldInitializer(u, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression), false);
        
        foreach(var c in classDeclarations.ToList())
        {
            BuildClassFieldInitializer(c.Key);
        }

        foreach (var p in classPreBuildProcessors)
        {
            foreach (var k in classDeclarations.Keys.ToList())
                p.Process(k, this);
        }

        var compilationUnit = SyntaxFactory.CompilationUnit();
        var constructedClasses = classDeclarations.Select(n => BuildClassConstructor(n.Key, n.Value));
        constructedClasses = constructedClasses.Select(AnnotateFieldTypeHints);

        compilationUnit = compilationUnit.AddMembers(constructedClasses.ToArray());
        compilationUnit = compilationUnit.AddMembers(methodClassDeclarations.Values.ToArray());
        compilationUnit = compilationUnit.AddMembers(methodClassArgumentBuilderDeclarations.Values.ToArray());
        compilationUnit = compilationUnit.AddMembers(methodExecutionContextClassDeclarations.Values.ToArray());

        foreach (var rewriter in Rewriters)
            compilationUnit = (CompilationUnitSyntax)rewriter.Visit(compilationUnit);

        return compilationUnit;
    }
}