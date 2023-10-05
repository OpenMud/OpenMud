using OpenMud.Mudpiler.RuntimeEnvironment.Operators;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;

namespace OpenMud.Mudpiler.Framework.Datums;

public class VarDmlCoord : DmlCoord
{
    public override EnvObjectReference x { get; } = VarEnvObjectReference.Variable(0);

    public override EnvObjectReference y { get; } = VarEnvObjectReference.Variable(0);

    public override EnvObjectReference z { get; } = VarEnvObjectReference.Variable(0);

    public override void _constructor()
    {
        base._constructor();

        //Technically this doesn't make too much sense, since the methods are being statically bound to the DmlList instance, and ignoring the self argument.
        //But it is a shortcut we can probably take without much issue.
        RegistedProcedures.Register(0, new ActionDatumProc("New", New));

        RegistedProcedures.Register(0, new ActionDatumProc("Assign",
                new[] { new BinOpAsnOverride(DmlBinaryAssignment.Assignment) },
                args => Assign(args[0])
            )
        );
    }

    public EnvObjectReference New(ProcArgumentList args)
    {
        var size = args.GetArgumentList();

        if (size.Length == 0)
            return VarEnvObjectReference.CreateImmutable(this);

        x.Assign(size[0]);
        y.Assign(size[1]);
        z.Assign(size[2]);

        return VarEnvObjectReference.CreateImmutable(this);
    }


    //[BinOpAsnOverride(DmlBinaryAssignment.Assignment)]
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