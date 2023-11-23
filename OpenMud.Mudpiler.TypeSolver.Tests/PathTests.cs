namespace OpenMud.Mudpiler.TypeSolver.Tests
{
    public class Tests
    {
        [Test]
        public void TestResolveParentRooted()
        {
            Assert.That(DmlPath.ResolveParentClass("/myClass"), Is.EqualTo("/datum"));
            Assert.That(DmlPath.ResolveParentClass("/myClass/andAnother"), Is.EqualTo("/datum/myClass"));
            Assert.That(DmlPath.ResolveParentClass("/"), Is.EqualTo(null));
            Assert.That(DmlPath.ResolveParentClass("/datum"), Is.EqualTo(null));
            Assert.That(DmlPath.ResolveParentClass("/atom"), Is.EqualTo(null));
            Assert.That(DmlPath.ResolveParentClass("/list"), Is.EqualTo(null));
            Assert.That(DmlPath.ResolveParentClass("/turf"), Is.EqualTo("/atom"));
            Assert.That(DmlPath.ResolveParentClass("/mob"), Is.EqualTo("/atom/movable"));
            Assert.That(DmlPath.ResolveParentClass("/obj"), Is.EqualTo("/atom/movable"));
            Assert.That(DmlPath.ResolveParentClass("/obj/testobj"), Is.EqualTo("/atom/movable/obj"));
        }

        [Test]
        public void TestResolveWithModifiers()
        {
            Assert.That(DmlPath.BuildQualifiedDeclarationName("/obj/testobj/static/test"), Is.EqualTo("/atom/movable/obj/testobj/test"));
            Assert.That(DmlPath.BuildQualifiedDeclarationName("/list"), Is.EqualTo("/list"));
        }

        [Test]
        public void EnumerateAliasTestArea()
        {
            var accepted = new HashSet<string>()
            {
                "/area/outside",
                "/atom/area/outside"
            };

            var aliasls = DmlPath.EnumerateTypeAliasesOf("/area/outside").ToList();
            var alias = aliasls.ToHashSet();

            Assert.IsTrue(alias.Count == aliasls.Count);

            Assert.IsTrue(alias.Count == accepted.Count && alias.Intersect(accepted).Count() == accepted.Count);
        }

        [Test]
        public void EnumerateAliasTestMob()
        {
            var accepted = new HashSet<string>()
            {
                "/mob/test",
                "/atom/movable/mob/test",
                "/movable/mob/test"
            };

            var aliasls = DmlPath.EnumerateTypeAliasesOf("/mob/test").ToList();
            var alias = aliasls.ToHashSet();

            Assert.IsTrue(alias.Count == aliasls.Count);


            Assert.IsTrue(alias.Count == accepted.Count && alias.Intersect(accepted).Count() == accepted.Count);
        }

        [Test]
        public void EnumerateAliasTestDatum()
        {
            var accepted = new HashSet<string>()
            {
                "/my_datum",
                "/datum/my_datum",
            };

            var aliasls = DmlPath.EnumerateTypeAliasesOf("/my_datum").ToList();
            
            var alias = aliasls.ToHashSet();

            Assert.IsTrue(alias.Count == aliasls.Count);

            Assert.IsTrue(alias.Count == accepted.Count && alias.Intersect(accepted).Count() == accepted.Count);
        }

        [Test]
        public void EnumerateAliasTestTurf()
        {
            var accepted = new HashSet<string>()
            {
                "/turf/test",
                "/atom/turf/test"
            };

            var aliasls = DmlPath.EnumerateTypeAliasesOf("/turf/test").ToList();
            var alias = aliasls.ToHashSet();

            Assert.IsTrue(alias.Count == aliasls.Count);

            Assert.IsTrue(alias.Count == accepted.Count && alias.Intersect(accepted).Count() == accepted.Count);
        }



        [Test]
        public void EnumerateAliasTestExpandedMob()
        {
            var accepted = new HashSet<string>()
            {
                "/atom/movable/mob",
                "/movable/mob",
                "/mob"
            };

            var aliasls = DmlPath.EnumerateTypeAliasesOf(DmlPath.BuildQualifiedDeclarationName("/mob")).ToHashSet();
            var alias = aliasls.ToHashSet();

            Assert.IsTrue(alias.Count == aliasls.Count);

            Assert.IsTrue(alias.Count == accepted.Count && alias.Intersect(accepted).Count() == accepted.Count);
        }

        [Test]
        public void EnumerateAliasDatum()
        {
            var accepted = new HashSet<string>()
            {
                "/myDatum",
                "/datum/myDatum"
            };

            var aliasls = DmlPath.EnumerateTypeAliasesOf("/myDatum").ToHashSet();
            var alias = aliasls.ToHashSet();

            Assert.IsTrue(alias.Count == aliasls.Count);

            Assert.IsTrue(alias.Count == accepted.Count && alias.Intersect(accepted).Count() == accepted.Count);
        }

        [Test]
        public void EnumerateAliasDatumSubProc()
        {
            var accepted = new HashSet<string>()
            {
                "/myDatum/testproc",
                "/myDatum/proc/testproc",
                "/myDatum/verb/testproc",
                "/datum/myDatum/testproc",
                "/datum/myDatum/proc/testproc",
                "/datum/myDatum/verb/testproc",
            };

            var alias = DmlPath.EnumerateTypeAliasesOf("/myDatum/proc/testproc").ToHashSet();
            var alias2 = DmlPath.EnumerateTypeAliasesOf("/myDatum/testproc", true).ToHashSet();

            Assert.IsTrue(alias.Count == accepted.Count && alias.Intersect(accepted).Count() == accepted.Count);
            Assert.IsTrue(alias2.Count == accepted.Count && alias2.Intersect(accepted).Count() == accepted.Count);
        }


        [Test]
        public void EnumerateAliasDatumProc()
        {
            var accepted = new HashSet<string>()
            {
                "/datum/proc/testproc",
                "/datum/verb/testproc",
                "/datum/testproc",
            };

            var aliasls = DmlPath.EnumerateTypeAliasesOf("/datum/proc/testproc").ToHashSet();
            var alias = aliasls.ToList();

            Assert.IsTrue(alias.Count == aliasls.Count);
            Assert.IsTrue(alias.Count == accepted.Count && alias.Intersect(accepted).Count() == accepted.Count);
        }


        [Test]
        public void EnumerateAliasDatumProc2()
        {
            var accepted = new HashSet<string>()
            {
                "/datum/proc/testproc",
                "/datum/verb/testproc",
                "/datum/testproc",
            };

            var aliasls = DmlPath.EnumerateTypeAliasesOf("/datum/testproc", true).ToHashSet();
            var alias = aliasls.ToList();

            Assert.IsTrue(alias.Count == aliasls.Count);
            Assert.IsTrue(alias.Count == accepted.Count && alias.Intersect(accepted).Count() == accepted.Count);
        }

        [Test]
        public void EnumerateAliasGlobalProc()
        {
            var accepted = new HashSet<string>()
            {
                "/testproc",
                "/proc/testproc",
                "/verb/testproc",
                "/GLOBAL/proc/testproc",
                "/GLOBAL/verb/testproc",
                "/GLOBAL/testproc",
            };

            var aliasls = DmlPath.EnumerateTypeAliasesOf("/proc/testproc").ToHashSet();
            var alias = aliasls.ToList();

            Assert.IsTrue(alias.Count == aliasls.Count);
            Assert.IsTrue(alias.Count == accepted.Count && alias.Intersect(accepted).Count() == accepted.Count);
        }

        [Test]
        public void EnumerateAliasGlobalProc2()
        {
            var accepted = new HashSet<string>()
            {
                "/testproc",
                "/proc/testproc",
                "/verb/testproc",
                "/GLOBAL/proc/testproc",
                "/GLOBAL/verb/testproc",
                "/GLOBAL/testproc",
            };

            var aliasls = DmlPath.EnumerateTypeAliasesOf("/GLOBAL/testproc", true).ToHashSet();
            var alias = aliasls.ToList();

            Assert.IsTrue(alias.Count == aliasls.Count);
            Assert.IsTrue(alias.Count == accepted.Count && alias.Intersect(accepted).Count() == accepted.Count);
        }


        [Test]
        public void ParsePathExtractMemberTest0()
        {
            DmlPath.ParseDeclarationPath("/atom/movable/mob/player/New", out var effectiveClassNameFullPath, out var methodName, true);

            Assert.IsTrue(effectiveClassNameFullPath == "/atom/movable/mob/player");
            Assert.IsTrue(methodName == "New");
        }

        [Test]
        public void TestResolvePathQualified()
        {
            Assert.That(DmlPath.BuildQualifiedNamespaceName("/proc/myProc"), Is.EqualTo("/GLOBAL/proc/myProc"));

            //Root procs are always translated to a namespace resolution.
            Assert.That(DmlPath.BuildQualifiedDeclarationName("/proc/myProc"), Is.EqualTo("/GLOBAL/proc/myProc"));
        }

        [Test]
        public void TestList()
        {
            // /list is a special case. It is not a user declared type, but it also does not have a base class of /datum

            Assert.That(DmlPath.ResolveParentClass("/list"), Is.EqualTo(null));
            Assert.That(DmlPath.IsPrimitiveClass("/list"), Is.EqualTo(false));

            Assert.That(DmlPath.BuildQualifiedDeclarationName("/list/t"), Is.EqualTo("/list/t"));
        }

        [Test]
        public void IDontEvenKnowWhatToCallThis()
        {
            //StaticTypeSolver.ParseNamespacePath(fullPath, out var effectiveClassNameFullPath, out var methodName, true);


            // /test_global needs to be /GLOBAL/test_global
            // /proc/myproc needs to be /GLOBAL/proc/myproc
            // /my_datum/myproc needs to be /datum/my_datum/myproc
            // /my_datum/q needs to be /datum/my_datum/q
            string className, componentName;

            DmlPath.ParseNamespacePath("/test_global", out className, out componentName, true);
            Assert.IsTrue(className == "/GLOBAL");
            Assert.IsTrue(componentName == "test_global");

            DmlPath.ParseNamespacePath("/proc/myproc", out className, out componentName, true);
            Assert.IsTrue(className == "/GLOBAL");
            Assert.IsTrue(componentName == "myproc");

            DmlPath.ParseNamespacePath("/my_datum/proc/myproc", out className, out componentName, true);
            Assert.IsTrue(className == "/datum/my_datum");
            Assert.IsTrue(componentName == "myproc");
        }

        [Test]
        public void InstanceOfGlobalTest()
        {

            Assert.IsTrue(DmlPath.IsDeclarationInstanceOfPrimitive("/GLOBAL/test", DmlPrimitive.Global));
        }

        [Test]
        public void TestEnumerateBaseTypes()
        {
            var baseTypes = DmlPath.EnumerateBaseTypes("/mob/test").ToList();
            Assert.IsTrue(baseTypes.SequenceEqual(new[] {
                DmlPrimitive.Mob,
                DmlPrimitive.Movable,
                DmlPrimitive.Atom
            }));
        }

        [Test]
        public void TestSubpath()
        {
            Assert.IsTrue(DmlPath.IsImmediateChild("/atom", "/atom/_constructor_fieldinit"));
            Assert.IsFalse(DmlPath.IsImmediateChild("/world/test", "/atom"));
            Assert.IsTrue(DmlPath.IsImmediateChild("/atom/movable", "/atom/movable/test"));
            Assert.IsFalse(DmlPath.IsImmediateChild("/atom", "/atom/movable/mob/testmob/New\r\n"));
        }

        [Test]
        public void TestRemoveTrailingModifiers()
        {
            Assert.IsTrue(DmlPath.RemoveTrailingModifiers("test") == "test");
            Assert.IsTrue(DmlPath.RemoveTrailingModifiers("test/global/static") == "test");
            Assert.IsTrue(DmlPath.RemoveTrailingModifiers("/test") == "/test");
            Assert.IsTrue(DmlPath.RemoveTrailingModifiers("/test/global/static") == "/test");
        }

        [Test]
        public void TestProcQualifiedDeclName()
        {
            var r = DmlPath.BuildQualifiedDeclarationName("/proc/test");
            Assert.IsTrue(DmlPath.BuildQualifiedDeclarationName(r) == "/GLOBAL/proc/test");
        }


        [Test]
        public void TestSystemTypes()
        {
            Assert.That(DmlPath.ResolveParentClass("/list"), Is.EqualTo(null));
            Assert.That(DmlPath.ResolveParentClass("/sound"), Is.EqualTo(null));
            Assert.That(DmlPath.ResolveParentClass("/primitive_coord"), Is.EqualTo(null));


            Assert.IsTrue(DmlPath.BuildQualifiedDeclarationName("/list") == "/list");
            Assert.IsTrue(DmlPath.BuildQualifiedDeclarationName("/sound") == "/sound");
            Assert.IsTrue(DmlPath.BuildQualifiedDeclarationName("/primitive_coord") == "/primitive_coord");
        }

    }
}