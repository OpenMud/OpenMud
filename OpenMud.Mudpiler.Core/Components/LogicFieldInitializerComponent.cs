using System.Collections.Immutable;

namespace OpenMud.Mudpiler.Core.Components;

public class LogicFieldInitializerComponent
{
    public IImmutableDictionary<string, object> FieldInitializers;

    public LogicFieldInitializerComponent(IDictionary<string, object> fieldInitializers)
    {
        FieldInitializers = fieldInitializers.ToImmutableDictionary();
    }
}