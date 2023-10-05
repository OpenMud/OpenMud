using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Core.Utils;

public class LogicDirectory
{
    private readonly Dictionary<Guid, EntityHandle> logicInstances = new();

    public EntityHandle this[Guid id] => logicInstances[id];
    public Guid this[EntityHandle id] => logicInstances.Where(x => x.Value == id).Select(x => x.Key).Single();

    public EntityHandle this[Datum id] => logicInstances
        .Where(x => typeof(Datum).IsAssignableFrom(x.Value.Type) && x.Value.Unwrap<Datum>() == id).Select(x => x.Value)
        .Single();

    public Guid Register(EntityHandle entity)
    {
        var id = Guid.NewGuid();
        logicInstances[id] = entity;
        return id;
    }

    public Guid Register(Guid guid, EntityHandle entity)
    {
        if (guid == Guid.Empty)
            return Register(entity);

        logicInstances[guid] = entity;
        return guid;
    }

    public void Remove(Guid id)
    {
        logicInstances.Remove(id);
    }

    public void Remove(EntityHandle entity)
    {
        var id = logicInstances.Where(x => x.Value == entity).Select(x => x.Key).Single();

        Remove(id);
    }

    public bool Contains(Guid guid)
    {
        return logicInstances.ContainsKey(guid);
    }

    public EntityHandle Lookup(Guid id)
    {
        return logicInstances[id];
    }
}