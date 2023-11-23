using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenMud.Mudpiler.TypeSolver
{
    public static class DmlPath
    {

        public static readonly string GLOBAL_PATH = "/GLOBAL";
        public static readonly string DEFAULT_ROOT_TYPE = "/datum";

        private static readonly HashSet<DmlPathModifier> ComponentDelimitingModifiers = new HashSet<DmlPathModifier>()
        {
             DmlPathModifier.Verb,
             DmlPathModifier.Proc
        };

        private static readonly HashSet<string> UserDeclarativeBaseClasses = new HashSet<string>()
        {
            "/atom",
            "/world",
            "/client",
            "/datum",
            "/GLOBAL"
        };

        private static readonly HashSet<string> SystemBaseClasses = new HashSet<string>()
        {
            "/list",
            "/sound",
            "/primitive_coord"
        };

        private static readonly Dictionary<string, DmlPrimitive> ImmediateBaseMapping = new()
        {
            { "/atom/movable/mob", DmlPrimitive.Mob },
            { "/atom/movable/obj", DmlPrimitive.Obj },
            { "/atom/movable", DmlPrimitive.Movable },
            { "/atom/turf", DmlPrimitive.Turf },
            { "/atom/area", DmlPrimitive.Area },
            { "/atom", DmlPrimitive.Atom },
            { "/world", DmlPrimitive.World },
            { "/client", DmlPrimitive.Client },
            { "/GLOBAL", DmlPrimitive.Global },
            { "/datum", DmlPrimitive.Datum },
            { "/list", DmlPrimitive.List }
        };

        private static readonly Dictionary<string, string> ClassPathExpansion = new()
        {
            { "/mob", "/atom/movable/mob" },
            { "/obj", "/atom/movable/obj" },
            { "/turf", "/atom/turf" },
            { "/area", "/atom/area" },
            { "/movable", "/atom/movable" }
        };


        private static readonly Dictionary<string, TypeSyntax> BasicTypes = new()
        {
            { "num", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword)) },
            { "text", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)) },
            { "message", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)) },
            { "anything", SyntaxFactory.ParseTypeName("dynamic") }
        };

        //All pre-defined system types (for example /atom is predefined, /atom/movable is not, there is no WorldPiece Movable)
        public static IEnumerable<string> UserDeclarablePrimitives => UserDeclarativeBaseClasses;

        //All types defined by default
        public static IEnumerable<string> PredefinedTypes = ClassPathExpansion.Keys.Concat(UserDeclarablePrimitives).Concat(SystemBaseClasses).Select(BuildQualifiedDeclarationName).Distinct();

        //All types defined by default which can be user overridden.
        public static IEnumerable<string> DefaultCompilerImplementationTypes = ClassPathExpansion.Keys.Concat(UserDeclarablePrimitives).Select(BuildQualifiedDeclarationName).Distinct();

        public static bool IsGlobal(string path) => NormalizeClassName(path) == GLOBAL_PATH;

        private static bool HasInitialComponent(string target, string comp)
        {
            if (target == comp)
                return true;

            return target.Length > comp.Length && target.ElementAt(comp.Length) == '/' && target.StartsWith(comp);
        }

        public static bool IsDeclarationInstanceOfPrimitive(string path, DmlPrimitive type)
        {
            var fullyQualifiedName = BuildQualifiedDeclarationName(path);

            foreach (var (k, v) in ImmediateBaseMapping)
            {
                if (v != type)
                    continue;

                return HasInitialComponent(fullyQualifiedName, k);
            }

            throw new Exception("Unknown primitive type.");
        }


        private static string[] Split(string fullPath)
        {
            return fullPath.Split('/').Where(x => x.Length > 0).ToArray();
        }


        public static List<string> ResolveInheritancePath(string name)
        {
            var parent = ResolveParentClass(name);

            if (parent == null)
                return new List<string>();

            return parent.Split("/").Where(x => x.Length > 0).ToList();
        }


        private static IEnumerable<string> EnumerateProcNameAliasesOf(string procName, bool isType)
        {
            List<string> ProduceProcAliases(string name)
            {
                var comps = Split(name).ToList();

                var tail = comps.Last();

                comps.RemoveAt(comps.Count - 1);

                var reserved = new[] { "verb", "proc" };

                if (comps.Any() && reserved.Contains(comps.Last()))
                    comps.RemoveAt(comps.Count - 1);

                return new[]
                    {
                    comps.Append(tail),
                    comps.Append("verb").Append(tail),
                    comps.Append("proc").Append(tail)
                }
                .Select(p => "/" + string.Join("/", p))
                .ToList();
            }

            var simple = SimpleEnumerateAliasesOf(procName, isType, true);

            return simple.Concat(
                    simple.SelectMany(ProduceProcAliases)
                )
                .Distinct()
                .ToList();
        }

        private static bool IsRootProc(string procName, bool assumeIsProc = true)
        {
            //In this context, we assume procName has been established as a path to a proc.
            return (assumeIsProc && 
                    (
                        Split(procName).Length == 1 ||
                        HasInitialComponent(procName, "/GLOBAL")
                    )
                ) || 
                HasInitialComponent(procName, "/proc") || 
                HasInitialComponent(procName, "/verb");
        }

        //I Feel like an AI Model being trained while I am writing this code because there are so many edge cases...
        private static IEnumerable<string> SimpleEnumerateAliasesOf(string className, bool isType, bool isProc = false)
        {
            bool isRootProc = false;

            if (isProc)
                isRootProc = IsRootProc(className);

            var baseName = (!isRootProc && isType) ? BuildQualifiedDeclarationName(className) : BuildQualifiedNamespaceName(className);

            yield return baseName;

            //With procs /proc/test belongs to global, not datum
            var rootScope = isRootProc ? GLOBAL_PATH : DEFAULT_ROOT_TYPE;

            if (HasInitialComponent(baseName, rootScope))
            {
                var sub = "/" + baseName.Substring(rootScope.Length).Trim('/');

                var allow = !isProc || (!IsRootProc(sub) || HasInitialComponent(baseName, GLOBAL_PATH));

                if(allow && sub.Trim('/').Length > 0)
                    yield return sub;
            }


            foreach (var (k, v) in ClassPathExpansion)
            {
                if (HasInitialComponent(baseName, v))
                {
                    var ext = baseName.Substring(v.Length).TrimStart('/');

                    if (ext.Length == 0)
                        yield return k;
                    else
                        yield return (k + "/" + ext);
                }
            }
        }

        private static IEnumerable<string> EnumerateAlisesOf(string className, bool isProc, bool isType)
        {
            var components = Split(className);

            if (!isProc)
                isProc = components.Any(x => x == "proc" || x == "verb");

            if (isProc)
                return EnumerateProcNameAliasesOf(className, isType);

            return SimpleEnumerateAliasesOf(className, isType);
        }

        //Resolves /proc/myProc to /datum/proc/myProc
        public static IEnumerable<string> EnumerateTypeAliasesOf(string className, bool isProc = false) =>
            EnumerateAlisesOf(className, isProc, true);

        //resolves /proc/myProc to /GLOBAL/proc/myProc

        public static IEnumerable<string> EnumeratePathAliasesOf(string pathName, bool isProc = false) =>
            EnumerateAlisesOf(pathName, isProc, false);


        public static bool IsPrimitiveClass(string className) {
            if (SystemBaseClasses.Contains(className))
                return false;

            return HasNoBaseClass(className);
        }

        private static string NormalizeClassName(string name, bool resolvingType = false)
        {
            var n = name.TrimEnd('/');

            if (n.Length == 0)
                return resolvingType ? DEFAULT_ROOT_TYPE : NormalizeClassName(GLOBAL_PATH);

            var isRooted = name.StartsWith("/");

            var simplified = string.Join("/", name.Split("/", StringSplitOptions.RemoveEmptyEntries));

            if (isRooted)
                simplified = "/" + simplified;

            return simplified;
        }


        private static DmlPathModifier? GetModifier(string name)
        {
            var modifier = ((DmlPathModifier[])Enum.GetValues(typeof(DmlPathModifier)))
                .Where(n => n.ToString().ToLower() == name).ToList();

            if (!modifier.Any())
                return null;

            return modifier.Single();
        }

        private static bool HasNoBaseClass(string name)
        {
            name = name.TrimEnd('/');

            return UserDeclarativeBaseClasses.Contains(name) || SystemBaseClasses.Contains(name);
        }

        private static bool DerivesFromBase(string path)
        {
            return SystemBaseClasses.Concat(UserDeclarativeBaseClasses).Any(c => HasInitialComponent(path, c));

        }

        public static string Concat(string a, string b)
        {
            if (b == null)
                return a;

            if (b.StartsWith("/"))
                return b;

            var r = string.Join("/", a.Split('/').Concat(b.Split("/")).Where(x => x.Length > 0));

            if (a.StartsWith("/"))
                r = "/" + r;

            return r;
        }

        private static string ExpandFullPath(string path, bool isDeclaration)
        {
            var root = isDeclaration ? DEFAULT_ROOT_TYPE : GLOBAL_PATH;

            if (!path.StartsWith("/"))
                path = "/" + path;

            //Check for any class expansions
            foreach (var ce in ClassPathExpansion)
            {
                if (HasInitialComponent(path, ce.Key))
                {
                    path = ce.Value + path.Substring(ce.Key.Length);
                    break;
                }
            }

            //Check path starts with at least one base type, and if not, append /datum (default base type.)
            var derivesBase = DerivesFromBase(path);

            if (HasInitialComponent(path, "/var"))
                throw new Exception("Invalid class name. This is a scope name.");

            if (!derivesBase)
                path = root.TrimEnd('/') + "/" + path.TrimStart('/');

            return path;
        }

        private static string BuildQualifiedName(string path, bool isDeclaration)
        {
            if (IsRootProc(path, false))
                isDeclaration = false;

            path = ExpandFullPath(path, isDeclaration);

            string effectiveClassNameFullPath, effectiveComponentName;
            var modifiers = isDeclaration ? ParseDeclarationPath(path, out effectiveClassNameFullPath, out effectiveComponentName) : ParseNamespacePath(path, out effectiveClassNameFullPath, out effectiveComponentName);

            var resultantPath = effectiveClassNameFullPath;

            //For naming, we keep the proc and verb modifiers before the effective componentName

            if (effectiveComponentName == null)
                return resultantPath;

            if (modifiers.Contains(DmlPathModifier.Verb))
                resultantPath = Concat(resultantPath, "verb");
            else if (modifiers.Contains(DmlPathModifier.Proc))
                resultantPath = Concat(resultantPath, "proc");

            return Concat(resultantPath, effectiveComponentName);
        }

        private static string BuildQualifiedName(string super, string sub, bool isDeclaration)
        {
            var l = BuildQualifiedName(super, isDeclaration);

            var r = l + (l.EndsWith('/') ? "" : "/") + sub;

            return r.TrimEnd('/');
        }

        public static string RootClassName(string name)
        {
            var s = NormalizeClassName(name, true);

            if (!s.StartsWith("/"))
                s = "/" + s;

            return s;
        }

        public static string BuildQualifiedNamespaceName(string path) => BuildQualifiedName(path, false);

        public static string BuildQualifiedPathName(string super, string sub) => BuildQualifiedName(super, sub, false);

        public static string BuildQualifiedDeclarationName(string super, string sub) => BuildQualifiedName(super, sub, true);

        public static string BuildQualifiedDeclarationName(string path) => BuildQualifiedName(path, true);

        public static string ExtractComponentName(string fullPath)
        {
            ParseDeclarationPath(fullPath, out var _, out var compName, true);

            return compName!;
        }

        /*
        public static string ResolveContainingClass(string fullPath)
        {
            var className = BuildQualifiedDeclarationName(fullPath);

            ParsePath(className, out var classComponent, out _);

            return className;
        }*/

        public static string? ResolveParentClass(string fullPath)
        {
            var className = BuildQualifiedDeclarationName(fullPath);

            ParseDeclarationPath(className, out var classComponent, out _);

            if (HasNoBaseClass(classComponent))
                return null;

            var r = className.Trim('/').Split('/');

            return "/" + string.Join('/', r.Take(r.Length - 1));
        }

        public static int ComputePathDepth(string absoluteName)
        {
            return BuildQualifiedDeclarationName(absoluteName).Split("/").Where(x => x.Length > 0).Count();
        }

        private static List<DmlPathModifier> ParsePath(string path, out string className, out string? componentName, bool mustHaveComponent, bool isDeclaration)
        {
            componentName = null;

            if (path == null)
                throw new Exception("Path cannot be null.");

            var rooted = path.StartsWith("/");

            var components = new Stack<string>(path.Split('/').Where(x => x.Length > 0));

            if (components.Count == 0)
            {
                className = isDeclaration ? BuildQualifiedDeclarationName(path) : BuildQualifiedNamespaceName(path);
                return new List<DmlPathModifier>();
            }

            var modifiers = new List<DmlPathModifier>();

            componentName = components.Pop();
            bool extractedModifiers = false;
            while (components.Count > 0 && GetModifier(components.Peek()).HasValue)
            {
                var modifier = GetModifier(components.Pop())!.Value;
                modifiers.Add(modifier);
                extractedModifiers = true;
            }

            if(!extractedModifiers)
            {
                components.Push(componentName);
                componentName = null;
            }

            if (!mustHaveComponent)
            {
                if (components.LastOrDefault() == "var")
                    mustHaveComponent = true;
                else if (modifiers.Contains(DmlPathModifier.Proc) || modifiers.Contains(DmlPathModifier.Verb) || modifiers.Contains(DmlPathModifier.Var))
                    mustHaveComponent = true;
            }

            if (mustHaveComponent && componentName == null)
                componentName = components.Pop();

            var newPathComponents = components.ToList().Reverse<string>();
            var newPath = string.Join("/", newPathComponents);

            if (rooted)
                newPath = "/" + newPath;

            className = ExpandFullPath(NormalizeClassName(newPath, isDeclaration), true);

            return modifiers;
        }

        public static List<DmlPathModifier> ParseNamespacePath(string path, out string className, out string? componentName, bool mustHaveComponent = false) =>
            ParsePath(path, out className, out componentName, mustHaveComponent, false);

        public static List<DmlPathModifier> ParseDeclarationPath(string path, out string className, out string? componentName, bool mustHaveComponent = false) =>
                    ParsePath(path, out className, out componentName, mustHaveComponent, true);


        private static List<DmlPathModifier> ParsePath(string path, out string fullyQualifiedName, bool isDeclaration)
        {
            var r = ParsePath(path, out var className, out var componentName, false, isDeclaration);

            if (componentName == null)
                fullyQualifiedName = className;
            else
                fullyQualifiedName = isDeclaration ? BuildQualifiedDeclarationName(className, componentName) : BuildQualifiedPathName(className, componentName);

            return r;
        }


        public static IEnumerable<DmlPrimitive> EnumerateBaseTypes(string path)
        {
            var pathBuffer = BuildQualifiedDeclarationName(path);

            while (pathBuffer != null)
                yield return ResolveImmediateBaseType(pathBuffer, out pathBuffer);
        }

        private static DmlPrimitive ResolveImmediateBaseType(string initialPath, out string? super)
        {
            super = null;

            var path = BuildQualifiedDeclarationName(initialPath);

            string? discovered = null;

            foreach (var p in ImmediateBaseMapping.Keys)
            {
                if (HasInitialComponent(path, p) && (discovered == null || p.Length > discovered.Length))
                    discovered = p;
            }

            if (discovered == null)
                return DmlPrimitive.Datum;

            super = ResolveParentClass(discovered);

            return ImmediateBaseMapping[discovered];
        }

        public static bool IsImmediateChild(string fullName, string subPath)
        {
            if (!fullName.StartsWith("/") || !subPath.StartsWith("/"))
                throw new ArgumentException();

            var fullNameNoModifiers = BuildQualifiedDeclarationName(fullName);
            var subPathNoModifiers = BuildQualifiedDeclarationName(subPath);

            if (ComputePathDepth(fullNameNoModifiers) != ComputePathDepth(subPathNoModifiers) - 1)
                return false;

            return HasInitialComponent(subPathNoModifiers, fullNameNoModifiers);
        }

        public static string RemoveTrailingModifiers(string parent)
        {
            var comps = Split(parent).ToList();

            while(comps.Count > 0 && GetModifier(comps.Last()) != null)
                comps.RemoveAt(comps.Count - 1);

            return (parent.FirstOrDefault() == '/' ? "/" : "") + string.Join('/', comps);
        }

        public static string NameWithModifiers(IEnumerable<DmlPathModifier> modifiers, string name)
        {
            return string.Join("/", modifiers.Select(m => m.ToString().ToLower()).Append(name));
        }

        public static List<DmlPathModifier> ParseNamespacePath(string path, out string fullyQualifiedName) => ParsePath(path, out fullyQualifiedName, false);
        public static List<DmlPathModifier> ParseDeclarationPath(string path, out string fullyQualifiedName) => ParsePath(path, out fullyQualifiedName, true);

        public static string BuildPath(IEnumerable<string> comps)
        {
            var p = new StringBuilder();

            foreach (var c in comps) {
                p.Append("/");
                p.Append(c);
            }

            return p.ToString();
        }
    }
}