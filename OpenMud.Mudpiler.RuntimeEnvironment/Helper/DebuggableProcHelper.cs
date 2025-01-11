using System.Reflection;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;

namespace OpenMud.Mudpiler.RuntimeEnvironment.Helper;

public struct DebugInformation
{
    public string FileName { init; get; }
    public int StartLine  { init; get; }
    public int EndLine  { init; get; }
    public int[] ExecutedLines  { init; get; }
}

public static class DebuggableProcHelper
{
    public static void RegisterStep(Type t, ProcStep d)
    {
        RegisterContextStep(LookupContextType(t), d);
    }
    
    public static void UnregisterStep(Type t, ProcStep d)
    {
        UnregisterContextStep(LookupContextType(t), d);
    }

    private static Type LookupContextType(Type t)
    {
        //While a DML Proc class can have multiple ProcDefintition clauses (i.e to register under an alias etc.)
        //they will all have the same ContextClass type, so we can pick any one.
        var attribute = t.GetCustomAttributes<ProcDefinition>().FirstOrDefault();

        if (attribute == null)
            throw new ArgumentException("The type provided must be of one which has the ProcDefinition attribute.");

        return attribute.ContextClass;
    }
    
    private static void RegisterContextStep(Type t, ProcStep d)
    {
        if (!t.IsAssignableTo(typeof(IDebuggableProc)))
            return;
        
        t.GetEvent(nameof(IDebuggableProc.Step))!.AddEventHandler(null, d);
    }
    
    private static void UnregisterContextStep(Type t, ProcStep d)
    {
        if (!t.IsAssignableTo(typeof(IDebuggableProc)))
            return;
        
        t.GetEvent(nameof(IDebuggableProc.Step))!.RemoveEventHandler(null, d);
    }

    public static DebugInformation CollectDebugInformation(Type t)
    {
        var contextType = LookupContextType(t);

        return new DebugInformation()
        {
            FileName = (string)contextType.GetProperty(nameof(IDebuggableProc.SourceFileName))!.GetValue(null)!
        };
    }

    public static DebugInformation CollectDebugInformation(IDebuggableProc i)
    {
        return CollectDebugInformation(i.GetType());
    }
}
