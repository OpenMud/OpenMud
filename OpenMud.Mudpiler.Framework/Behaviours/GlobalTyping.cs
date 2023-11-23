using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Proc;
using OpenMud.Mudpiler.RuntimeEnvironment.RuntimeTypes;
using OpenMud.Mudpiler.RuntimeEnvironment.Settings;
using OpenMud.Mudpiler.RuntimeEnvironment.Utils;
using OpenMud.Mudpiler.RuntimeEnvironment.WorldPiece;
using OpenMud.Mudpiler.TypeSolver;
using System.Collections.Generic;
using System.Reflection;

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
        procedureCollection.Register(0, new ActionDatumProc(RuntimeFrameworkIntrinsic.INDIRECT_ISTYPE, (args, datum) => indirect_istype(args[0], args[1])));
        procedureCollection.Register(0, new ActionDatumProc("ismob", (args, datum) => ismob(args[0])));
        procedureCollection.Register(0, new ActionDatumProc("assert_primitive", (args, datum) => assert_primitive(args[0], args[1])));
        procedureCollection.Register(0, new ActionDatumProc(RuntimeFrameworkIntrinsic.INDIRECT_NEW, (args, datum) => indirect_new(args[0], args[1], args.Split(2).last)));
    }

    private EnvObjectReference assert_primitive(EnvObjectReference subject, EnvObjectReference typeList)
    {
        var types = typeList.Get<DmlList>().Host.Select(v => DmlEnv.AsText(v.Key)).Where(t => t != null).ToList();

        foreach (var t in types)
        {
            if (DmlEnv.TestPrimitiveType(subject, t!))
                return subject;
        }

        throw new DmlRuntimeAssertionError("Primtiive type assertion failed.");
    }

    public bool AcceptsDatum(string target)
    {
        return DmlPath.IsDeclarationInstanceOfPrimitive(target, DmlPrimitive.Global);
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
        if (subject == null || subject.IsNull || t == null || t.IsNull)
            return false;

        //Everything is an object in DML runtime.
        if (typeof(object) == t.Get<Type>())
            return true;

        return t.Get<Type>().IsAssignableFrom(subject.Type);
    }

    public bool indirect_istype(EnvObjectReference subject, EnvObjectReference field)
    {
        if (subject == null || subject.IsNull || field == null || field.IsNull)
            return false;

        var hostClassInstance = subject.GetOrDefault<Datum>(null);

        if (hostClassInstance == null)
            return false;

        var hostClassType = hostClassInstance.GetType();
        var fld = hostClassType.GetField(field.GetOrDefault<string>(""));

        if (fld == null)
            return false;

        var fieldContents = fld.GetValue(hostClassInstance);

        if (fieldContents is EnvObjectReference r)
            fieldContents = r.IsNull ? null : r.Target;

        if (fieldContents == null)
            return false;

        var contentsType = fieldContents.GetType();

        var typeHintAttribute = fld.GetCustomAttribute<FieldTypeHint>();

        if (typeHintAttribute == null)
            return false;

        var hintedType = typeSolver.LookupOrDefault(typeHintAttribute.TypeName);

        if (hintedType == null)
            return false;

        return hintedType == contentsType || contentsType.IsSubclassOf(hintedType);
    }

    public EnvObjectReference indirect_new(EnvObjectReference subject, EnvObjectReference field, ProcArgumentList args)
    {
        if (subject == null || subject.IsNull || field == null || field.IsNull)
            throw new DmlRuntimeError("The host or field parameters are null but must be specified.");

        var hostClassInstance = subject.GetOrDefault<Datum>(null);

        if (hostClassInstance == null)
            throw new DmlRuntimeError("The host object is not a valid DML type.");

        var hostClassType = hostClassInstance.GetType();
        var fld = hostClassType.GetField(field.GetOrDefault<string>(""));

        if (fld == null)
            throw new DmlRuntimeError("The field to emplace the instantiated value does not exist on the specified type.");

        var typeHintAttribute = fld.GetCustomAttribute<FieldTypeHint>();

        if (typeHintAttribute == null)
            throw new DmlRuntimeError("Cannot indirect instantiate on this field because it has no associated type hint.");

        var hintedType = typeSolver.LookupOrDefault(typeHintAttribute.TypeName);

        if (hintedType == null)
            throw new DmlRuntimeError("Cannot indirect instantiate on this field the associated type could not be located in the runtime type library.");

        var r = instantiator(hintedType, args);

        fld.SetValue(hostClassInstance, r);

        return r;
    }

    public bool ismob(EnvObjectReference subject)
    {
        return DmlPath.IsDeclarationInstanceOfPrimitive(
            subject.Get<Datum>().type.Get<string>(),
            DmlPrimitive.Mob
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
        {
            foreach (var t in typeSolver.SubClasses(a))
                r.Add(VarEnvObjectReference.CreateImmutable(t));
        }

        return VarEnvObjectReference.CreateImmutable(r);
    }
}