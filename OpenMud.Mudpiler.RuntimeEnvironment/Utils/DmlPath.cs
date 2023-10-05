namespace OpenMud.Mudpiler.RuntimeEnvironment.Utils;

public static class DmlPath
{
    public static readonly string GLOBAL_PATH = "/GLOBAL";

    private static DmlPathModifier? GetModifier(string name)
    {
        var modifier = ((DmlPathModifier[])Enum.GetValues(typeof(DmlPathModifier)))
            .Where(n => n.ToString().ToLower() == name).ToList();

        if (!modifier.Any())
            return null;

        return modifier.Single();
    }

    public static List<DmlPathModifier> ExtractTailModifiers(string path, out string resolvedPath,
        bool keepTailName = true)
    {
        if (path == null)
            throw new Exception("Path cannot be null.");

        var rooted = path.StartsWith("/");

        var components = new Stack<string>(path.Split('/').Where(x => x.Length > 0));

        if (components.Count == 0)
        {
            resolvedPath = path;
            return new List<DmlPathModifier>();
        }

        var modifiers = new List<DmlPathModifier>();
        var tailName = components.Pop();

        if (GetModifier(tailName).HasValue)
            throw new Exception("Invalid tail name for modifier list!");

        while (components.Count > 0 && GetModifier(components.Peek()).HasValue)
            modifiers.Add(GetModifier(components.Pop()).Value);

        var newPathComponents = components.ToList().Reverse<string>();

        if (keepTailName)
            newPathComponents = newPathComponents.Append(tailName);

        var newPath = string.Join("/", newPathComponents);

        if (rooted)
            newPath = "/" + newPath;

        resolvedPath = newPath;

        return modifiers;
    }

    public static string RemoveModifiers(string fullPath)
    {
        var isRooted = fullPath.StartsWith("/");

        var newPath = string.Join("/", fullPath.Split("/").Where(x => x.Length > 0 && !GetModifier(x).HasValue));

        if (isRooted)
            newPath = "/" + newPath;

        return newPath;
    }

    public static string ResolveParentPath(string fullPath)
    {
        if (NormalizeClassName(fullPath) == NormalizeClassName(GLOBAL_PATH))
            return null;

        ExtractTailModifiers(fullPath, out var parent, false);

        return parent;
    }

    public static string ResolveBaseName(string fullPath)
    {
        return fullPath.Split('/').Where(x => x.Length > 0).Prepend("").Last();
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

    public static string[] Split(string fullPath)
    {
        return fullPath.Split('/').Where(x => x.Length > 0).ToArray();
    }

    public static string RootClassName(string name)
    {
        var s = NormalizeClassName(name);

        if (!s.StartsWith("/"))
            s = "/" + s;

        return s;
    }

    public static string NormalizeClassName(string name)
    {
        var n = name.TrimEnd('/');

        if (n.Length == 0)
            return NormalizeClassName(GLOBAL_PATH);

        var isRooted = name.StartsWith("/");

        var simplified = string.Join("/", name.Split("/", StringSplitOptions.RemoveEmptyEntries));

        if (isRooted)
            simplified = "/" + simplified;

        return simplified;
    }

    public static bool IsRoot(string baseClass)
    {
        return NormalizeClassName(GLOBAL_PATH) == NormalizeClassName(baseClass);
    }

    public static List<string> ResolveInheritancePath(string name)
    {
        var parent = ResolveParentPath(RemoveModifiers(name));

        if (parent == null)
            return new List<string>();

        return parent.Split("/").Where(x => x.Length > 0).ToList();
    }

    public static int ComputePathDepth(string absoluteName)
    {
        return NormalizeClassName(absoluteName).Split("/").Where(x => x.Length > 0).Count();
    }

    public static IEnumerable<string> EnumerateBasePathsDestruct(string className, bool enumerateRoot = true)
    {
        className = NormalizeClassName(RemoveModifiers(className));

        while (className != null)
        {
            if (!IsRoot(className) || enumerateRoot)
                yield return className;

            className = ResolveParentPath(className);
        }
    }

    public static IEnumerable<string> EnumerateBasePathsConstruct(string className, bool enumerateRoot = true)
    {
        return EnumerateBasePathsDestruct(className, enumerateRoot).Reverse();
    }

    public static string NameWithModifiers(IEnumerable<DmlPathModifier> modifiers, string name)
    {
        return string.Join("/", modifiers.Select(m => m.ToString().ToLower()).Append(name));
    }

    public static string RemoveTrailingModifiers(string parent)
    {
        ExtractTailModifiers(Concat(parent, "_"), out var resolved, false);

        return resolved;
    }

    public static bool IsImmediateChild(string fullName, string subPath)
    {
        if (!fullName.StartsWith("/") || !subPath.StartsWith("/"))
            throw new ArgumentException();

        var fullNameNoModifiers = RemoveModifiers(fullName);
        var subPathNoModifiers = RemoveModifiers(subPath);

        if (ComputePathDepth(fullNameNoModifiers) != ComputePathDepth(subPathNoModifiers) - 1)
            return false;

        return subPathNoModifiers.StartsWith(fullNameNoModifiers);
    }
}