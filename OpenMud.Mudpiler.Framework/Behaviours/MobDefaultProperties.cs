﻿using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;
using OpenMud.Mudpiler.TypeSolver;

namespace OpenMud.Mudpiler.Framework.Behaviours;

internal class MobDefaultProperties : IRuntimeTypeBuilder
{
    public bool AcceptsDatum(string target)
    {
        return DmlPath.IsDeclarationInstanceOfPrimitive(target, DmlPrimitive.Mob);
    }

    public void Build(DatumHandle e, Datum datum, DatumProcCollection procedureCollection)
    {
        var mob = (Atom)datum;
        mob.density.Assign(1.0);

        procedureCollection.Register(0, new ActionDatumProc("Login", args => VarEnvObjectReference.NULL));
    }
}