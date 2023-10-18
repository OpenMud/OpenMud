using OpenMud.Mudpiler.RuntimeEnvironment.Operators;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenMud.Mudpiler.Core.Messages;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Datums
{
    public class SoundInfo : Datum
    {
        public EnvObjectReference file { get; } = VarEnvObjectReference.Variable("");

        public EnvObjectReference repeat { get; } = VarEnvObjectReference.Variable(0);

        public EnvObjectReference channel { get; } = VarEnvObjectReference.Variable(0);
        public EnvObjectReference volume { get; } = VarEnvObjectReference.Variable(100);
        public EnvObjectReference frequency { get; } = VarEnvObjectReference.Variable(0);
        public EnvObjectReference wait { get; } = VarEnvObjectReference.Variable(0);
        public EnvObjectReference priority { get; } = VarEnvObjectReference.Variable(0);

        public override void _constructor()
        {
            base._constructor();

            //Technically this doesn't make too much sense, since the methods are being statically bound to the DmlList instance, and ignoring the self argument.
            //But it is a shortcut we can probably take without much issue.
            RegistedProcedures.Register(0, new ActionDatumProc("New", New));
        }

        public EnvObjectReference New(ProcArgumentList args)
        {
            if(args.MaxPositionalArgument > 0)
                file.Assign(args[0]);

            return VarEnvObjectReference.CreateImmutable(this);
        }
    }
}
