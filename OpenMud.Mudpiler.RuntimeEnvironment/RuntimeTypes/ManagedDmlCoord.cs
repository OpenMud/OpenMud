using System.Diagnostics.CodeAnalysis;
using OpenMud.Mudpiler.RuntimeEnvironment.Operators;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;

namespace OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

public struct SimpleDmlCoord
{
    public int x;
    public int y;
    public int z;

    public SimpleDmlCoord(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is SimpleDmlCoord c && c.x == x && c.y == y && c.z == z;
    }

    public override int GetHashCode()
    {
        return (x + 3) * (y + 1) * (z + 2);
    }

    public SimpleDmlCoord Replace(int replaceIdx, int val)
    {
        var c = new SimpleDmlCoord();
        c.x = x;
        c.y = y;
        c.z = z;

        switch (replaceIdx)
        {
            case 0:
                c.x = val;
                break;
            case 1:
                c.y = val;
                break;
            case 2:
                c.z = val;
                break;
            default:
                throw new Exception("Invalid Index");
        }

        return c;
    }
}

public class ManagedDmlCoord : DmlCoord
{
    public ManagedDmlCoord(Func<SimpleDmlCoord> getter, Action<SimpleDmlCoord> setter)
    {
        ManagedX.Bind(new ManagedEnvObjectReference(() => getter().x, v => setter(getter().Replace(0, (int)v))));
        ManagedY.Bind(new ManagedEnvObjectReference(() => getter().y, v => setter(getter().Replace(1, (int)v))));
        ManagedZ.Bind(new ManagedEnvObjectReference(() => getter().z, v => setter(getter().Replace(2, (int)v))));
    }

    public override EnvObjectReference x => ManagedX;
    public override EnvObjectReference y => ManagedY;
    public override EnvObjectReference z => ManagedZ;

    public ManagedEnvObjectReference ManagedX { get; } = new();
    public ManagedEnvObjectReference ManagedY { get; } = new();
    public ManagedEnvObjectReference ManagedZ { get; } = new();

    public override void _constructor()
    {
        RegistedProcedures.Register(0, new ActionDatumProc("Assign",
                new[] { new BinOpAsnOverride(DmlBinaryAssignment.Assignment) },
                args => Assign(args[0])
            )
        );

        base._constructor();
    }

    public EnvObjectReference Assign(EnvObjectReference other)
    {
        if (other.Target is DmlCoord c)
        {
            x.Assign(c.x);
            y.Assign(c.y);
            z.Assign(c.z);

            return VarEnvObjectReference.CreateImmutable(this);
        }

        throw new Exception("Cannot assign to value of this type.");
    }
}