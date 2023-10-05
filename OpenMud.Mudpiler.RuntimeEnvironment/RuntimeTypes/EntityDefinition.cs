namespace OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class EntityDefinition : Attribute
{
    public EntityDefinition(string name)
    {
        Name = name;
    }

    public string Name { get; }
}