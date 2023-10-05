namespace OpenMud.Mudpiler.RuntimeEnvironment.Proc;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class ProcDefinition : Attribute
{
    public ProcDefinition(string name, int declarationOrder)
    {
        Name = name;
        DeclarationOrder = declarationOrder;
    }

    public string Name { get; }
    public int DeclarationOrder { get; }
}