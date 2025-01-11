namespace OpenMud.Mudpiler.RuntimeEnvironment.Proc;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class ProcDefinition : Attribute
{
    public ProcDefinition(string name, int declarationOrder, Type contextClass)
    {
        Name = name;
        DeclarationOrder = declarationOrder;
        ContextClass = contextClass;
    }

    public string Name { get; }
    public int DeclarationOrder { get; }
    public Type ContextClass { get; }
}
