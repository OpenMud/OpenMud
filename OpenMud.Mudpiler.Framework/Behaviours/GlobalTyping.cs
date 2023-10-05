using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;

namespace OpenMud.Mudpiler.Framework.Behaviours;

internal class GlobalTyping : IRuntimeTypeBuilder
{
    private readonly ITypeSolver typeSolver;
    private readonly ObjectInstantiator instantiator;

    public GlobalTyping(ITypeSolver typeSolver, ObjectInstantiator instantiator)
    {
        this.typeSolver = typeSolver;
        this.instantiator = instantiator;
    }


    public void Build(DatumHandle e, Datum datum, DatumProcCollection procedureCollection)
    {
        procedureCollection.Register(0, new ActionDatumProc("typesof", typesof));
        procedureCollection.Register(0, new ActionDatumProc("locate", (args, datum) => locate(datum, args)));
        procedureCollection.Register(0, new ActionDatumProc("newlist", (args, datum) => newlist(datum, args)));
        procedureCollection.Register(0, new ActionDatumProc("istype", (args, datum) => istype(args[0], args[1])));
        procedureCollection.Register(0, new ActionDatumProc("ismob", (args, datum) => ismob(args[0])));
    }

    public bool AcceptsDatum(string target)
    {
        return RuntimeTypeResolver.HasImmediateBaseTypeDatum(target, DmlPrimitiveBaseType.Global);
    }

    private dynamic ResolveType(EnvObjectReference name)
    {
        if (typeof(Type).IsAssignableFrom(name.Type))
            return VarEnvObjectReference.CreateImmutable(name);

        return VarEnvObjectReference.CreateImmutable(typeSolver.Lookup(name.Get<string>()));
    }

    private Type[] ParseTypes(EnvObjectReference[] t)
    {
        var tlist = new List<Type>();
        foreach (var o in t)
            if (typeof(string).IsAssignableFrom(o.Type))
                tlist.Add(ResolveType(o.Get<string>()));
            else
                tlist.Add(o.Get<Type>());

        return tlist.ToArray();
    }

    public bool istype(EnvObjectReference subject, EnvObjectReference t)
    {
        if (subject == null || subject.IsNull)
            return false;

        return t.Get<Type>().IsAssignableFrom(subject.Type);
    }

    public bool ismob(EnvObjectReference subject)
    {
        return RuntimeTypeResolver.InheritsBaseTypeDatum(
            subject.Get<Datum>().type.Get<string>(),
            DmlPrimitiveBaseType.Mob
        );
    }

    public EnvObjectReference newlist(Datum datum, ProcArgumentList args)
    {
        var typeList = ParseTypes(args.GetArgumentList());
        var lst = instantiator(typeSolver.Lookup("/list"));
        var innerList = lst.Get<DmlList>();

        foreach (var a in typeList)
            innerList.Add(
                (EnvObjectReference)VarEnvObjectReference.CreateImmutable(
                    datum.ctx.NewAtomic(VarEnvObjectReference.CreateImmutable(a))));

        return VarEnvObjectReference.CreateImmutable(lst);
    }

    public EnvObjectReference locate(Datum datum, ProcArgumentList args)
    {
        if (args.MaxPositionalArgument == 1)
        {
            if (typeof(Type).IsAssignableFrom(args[0].Type))
            {
                var r = (DmlList)((EnvObjectReference)datum.ctx.EnumerateInstancesOf(args[0])).Target;

                if (!r.Any())
                    return VarEnvObjectReference.CreateImmutable(null);

                return VarEnvObjectReference.CreateImmutable(r.First());
            }

            throw new NotImplementedException();
        }


        throw new NotImplementedException();
    }


    public EnvObjectReference typesof(ProcArgumentList args)
    {
        var typeList = ParseTypes(args.GetArgumentList());
        var r = instantiator(typeSolver.Lookup("/list")).Get<DmlList>();

        foreach (var a in typeList)
        foreach (var t in typeSolver.SubClasses(a))
            r.Add(VarEnvObjectReference.CreateImmutable(t));

        return VarEnvObjectReference.CreateImmutable(r);
    }
}