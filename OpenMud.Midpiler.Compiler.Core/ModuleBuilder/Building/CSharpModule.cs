using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.CodeRewriters;
using OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.PrebuildProcessor;
using OpenMud.Mudpiler.Compiler.Core.Visitor;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;

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
    private readonly Dictionary<ModuleMethodDeclarationKey, ClassDeclarationSyntax> methodClassDeclarations = new();
    private readonly Dictionary<ModuleMethodDeclarationKey, MethodDeclarationSyntax> methodDeclarations = new();

    private readonly Dictionary<ModuleMethodDeclarationKey, ClassDeclarationSyntax>
        methodExecutionContextClassDeclarations = new();

    private readonly Dictionary<string, string> primitiveClassNames = new();
    private readonly Dictionary<string, Dictionary<string, AttributeSyntax>> settings = new();

    private readonly CSharpSyntaxRewriter[] Rewriters;

    public CSharpModule()
    {
        foreach (var t in BuiltinTypes.PredefinedTypes)
            primitiveClassNames[t] = DefineClass(t, c => c).Identifier.Text;

        Rewriters = new CSharpSyntaxRewriter[]
        {
            new IsTypeResolverRewriter(LookUpClass, LookUpClassName),
            new ClassNamespaceResolverWalker(),
            new InheritanceGraphRewriter(LookUpClass, p => primitiveClassNames[BuiltinTypes.ResolveClassAlias(p)],
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
            new SourceMapperRewriter()
        };
    }

    public IEnumerable<ModuleMethodDeclarationKey> MethodDeclarations => methodDeclarations.Keys.ToList();

    public void DefineClassMethod(string fullPath, Func<MethodDeclarationSyntax, MethodDeclarationSyntax> decl,
        int declarationOrder)
    {
        if (fullPath == null || fullPath.Length == 0)
            throw new Exception("Method name cannot be blank or null...");

        var modifiers = DmlPath.ExtractTailModifiers(fullPath, out var effectiveFullPath);

        effectiveFullPath = BuiltinTypes.ResolveClassAlias(DmlPath.RootClassName(effectiveFullPath));

        var methodName = DmlPath.ResolveBaseName(effectiveFullPath);

        if (methodName.Length == 0)
            throw new ArgumentException("Invalid method name.");

        //Define all of the base classes..
        var baseClassName = DmlPath.ResolveParentPath(effectiveFullPath);

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

        var declarationKey = new ModuleMethodDeclarationKey(declarationOrder, effectiveFullPath);

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

        var baseClass = Touch(baseClassName);

        var methodClassName = baseClass.Identifier.Text + "_" + methodName + "_" + declarationOrder;

        methodExecutionContextClassDeclarations[declarationKey] =
            DmlMethodBuilder.BuildMethodExecutionContextClass(methodDeclarations[declarationKey], baseClassName,
                methodClassName);
        methodClassDeclarations[declarationKey] = DmlMethodBuilder.BuildMethodClass(baseClassName, methodClassName,
            declarationOrder, methodDeclarations[declarationKey],
            methodExecutionContextClassDeclarations[declarationKey]);
    }

    public ClassDeclarationSyntax Touch(string path)
    {
        return DefineClass(path, c => c);
    }

    public void DefineClassField(string fullPath, string typeHint, Func<FieldDeclarationSyntax, FieldDeclarationSyntax> decl)
    {
        if (fullPath == null || fullPath.Length == 0)
            throw new Exception("Field name cannot be blank or null...");

        var modifiers = DmlPath.ExtractTailModifiers(fullPath, out var effectiveFullPath);

        var fieldName = DmlPath.ResolveBaseName(effectiveFullPath);

        if (fieldName.Length == 0)
            throw new ArgumentException("Invalid field name.");

        //Define all of the base classes..
        var baseClassName = BuiltinTypes.ResolveClassAlias(DmlPath.ResolveParentPath(effectiveFullPath));

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

        var absoluteName = BuiltinTypes.ResolveClassAlias(DmlPath.RootClassName(DmlPath.RemoveModifiers(fullPath)));

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
        var hostPath = BuiltinTypes.ResolveClassAlias(
            DmlPath.RootClassName(DmlPath.RemoveModifiers(string.Join("/", components.Take(components.Count - 1)))));

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
        var fieldName = DmlPath.ResolveBaseName(fullPath);
        var parent = BuiltinTypes.ResolveClassAlias(DmlPath.RootClassName(DmlPath.ResolveParentPath(fullPath)));

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
        var parent = BuiltinTypes.ResolveClassAlias(DmlPath.RootClassName(baseClass));

        //Make sure the class is initialized.
        DefineClass(parent, c => c);

        var supportIdx = 0;
        Func<int, string> supportName = idx => DmlPath.Concat(parent, $"__SUPPORT{idx}");

        var declKey = new ModuleMethodDeclarationKey(0, supportName(supportIdx));

        while (methodDeclarations.ContainsKey(declKey))
        {
            supportIdx++;
            declKey = new ModuleMethodDeclarationKey(0, supportName(supportIdx));
        }

        var selectedName = declKey.Name;

        DefineClassMethod(selectedName, decl, 0);

        return DmlPath.ResolveBaseName(selectedName);
    }

    private ClassDeclarationSyntax LookUpClass(string n)
    {
        return classDeclarations[BuiltinTypes.ResolveClassAlias(DmlPath.RootClassName(n))];
    }

    private string LookUpClassName(ClassDeclarationSyntax n)
    {
        return classDeclarations
            .Where(y => y.Value.Identifier.Text == BuiltinTypes.ResolveClassAlias(n.Identifier.Text)).Single().Key;
    }

    private ClassDeclarationSyntax DefineClass(string? fullPath, Func<ClassDeclarationSyntax, ClassDeclarationSyntax> define)
    {
        if (fullPath == null)
            fullPath = DmlPath.GLOBAL_PATH;

        //Define all of the base classes..
        var pathName = DmlPath.RootClassName(DmlPath.RemoveModifiers(fullPath));
        pathName = BuiltinTypes.ResolveClassAlias(pathName);

        if (pathName.Length == 0)
            throw new ArgumentException("Invalid class name path.");

        var baseClassName = DmlPath.ResolveParentPath(pathName);
        if (baseClassName != null)
            DefineClass(baseClassName, c => c);

        if (!classDeclarations.TryGetValue(pathName, out var decl))
        {
            var clsName = DmlPath.ResolveBaseName(pathName);

            if (!DmlPath.IsRoot(pathName))
                clsName += $"_{classDeclarations.Count}";

            decl = SyntaxFactory.ClassDeclaration(
                    clsName
                )
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));

            var classNames = BuiltinTypes.EnumerateAliasesOf(pathName);

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
        var absoluteName = BuiltinTypes.ResolveClassAlias(DmlPath.RootClassName(DmlPath.Concat(clsName, memberName)));

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

    private BlockSyntax CreateConstructor(string fullName)
    {
        var statements = new List<StatementSyntax>();


        if (fieldInitializers.TryGetValue(fullName, out var fields))
            foreach (var f in fields)
                statements.Add(
                    SyntaxFactory.ExpressionStatement(
                        EXPR.CreateAssignment(SyntaxFactory.IdentifierName(f.Key), f.Value)
                    )
                );

        var depth = DmlPath.ComputePathDepth(fullName);

        var childMethods = methodClassDeclarations.Where(x =>
            (DmlPath.IsRoot(fullName) && DmlPath.IsRoot(DmlPath.ResolveParentPath(x.Key.Name))) ||
            DmlPath.ResolveParentPath(x.Key.Name) == fullName
        ).OrderBy(m => m.Key.DeclarationOrder);

        foreach (var (m, cls) in childMethods)
        {
            var methodName = DmlPath.ResolveBaseName(m.Name);

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

    private ClassDeclarationSyntax BuildClassFieldsConstructor(string name, ClassDeclarationSyntax builtClass)
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

        foreach (var p in classPreBuildProcessors)
        {
            foreach (var k in classDeclarations.Keys.ToList())
                p.Process(k, this);
        }

        var compilationUnit = SyntaxFactory.CompilationUnit();
        var constructedClasses = classDeclarations.Select(n => BuildClassFieldsConstructor(n.Key, n.Value));
        constructedClasses = constructedClasses.Select(AnnotateFieldTypeHints);

        compilationUnit = compilationUnit.AddMembers(constructedClasses.ToArray());
        compilationUnit = compilationUnit.AddMembers(methodClassDeclarations.Values.ToArray());
        compilationUnit = compilationUnit.AddMembers(methodExecutionContextClassDeclarations.Values.ToArray());

        foreach (var rewriter in Rewriters)
            compilationUnit = (CompilationUnitSyntax)rewriter.Visit(compilationUnit);

        return compilationUnit;
    }
}