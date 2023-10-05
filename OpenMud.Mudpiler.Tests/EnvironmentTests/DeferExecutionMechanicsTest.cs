namespace OpenMud.Mudpiler.Tests.EnvironmentTests;


/*
internal class DeferExecutionMechanicsTest
{
    public class TestMethodContext : DmlDatumProcExecutionContext
    {
        private int localarg_a;
        private int localarg_b;

        private int localvar_a;
        private int localvar_b;
        private int localvar_c;
        private int localvar_d;
        private bool localvar_f;

        private DmlDeferredEvaluation invokePointA;
        private DmlDeferredEvaluation invokePointA_NestedA;
        private DmlDeferredEvaluation invokePointB;

        private List<string> execTrace;

        public TestMethodContext(List<string> execTrace) {
            this.execTrace = execTrace;
        }

        private static bool AlwaysTrue() => true;

        protected override object DoContinue()
        {
            switch (currentStep)
            {
                case 0:
                    IL.Emit.Br("exec_0");
                    break;
                case 1:
                    IL.Emit.Br("exec_1");
                    break;
                case 2:
                    IL.Emit.Br("exec_2");
                    break;
                default:
                    throw new Exception("Unknown execution origin...");
            }
            IL.MarkLabel("exec_0");
            execTrace.Add("Top 0");

            //This is basically if you do something like a(b(var_a)); -> b is a dependency of a
            invokePointA = CreateDeferred(
                new[] {
                    CreateDeferred((deps) => new NestedTestMethod2(execTrace).Create().SetupContext(null, null, null, 0, new ProcArgumentList(localvar_a))),
                },
                (deps) => new NestedTestMethod(execTrace).Create().SetupContext(null, null, null, 0, new ProcArgumentList(deps[0], localvar_b))
            );

            invokePointB = CreateDeferred(
                (deps) => new NestedTestMethod(execTrace).Create().SetupContext(null, null, null, 0, new ProcArgumentList(localvar_c, localvar_d)));

            currentStep = 0;
            localvar_a = localarg_a;
            localvar_b = localarg_b;
            localvar_b += 10;

            currentStep = 1;

            if (AlwaysTrue())
                throw new DeferExecutionException(DatumProcExecutionState.RUNNING);

            IL.MarkLabel("exec_1");
            execTrace.Add("Top 1");
            localvar_a *= 18;
            localvar_c = localvar_a ^ localvar_b;

            currentStep = 2;
            IL.MarkLabel("exec_2");
            execTrace.Add("Top 2");
            localvar_f = ((EnvObjectReference)invokePointA.Execute()).Get<int>() > 5 && ((EnvObjectReference)invokePointB.Execute()).Get<int>() > 3;

            execTrace.Add($"Top: {localarg_a}, {localarg_b}, {localvar_a}, {localvar_b}, {localvar_c}, {localvar_d}, {localvar_f}");
            return localvar_f;

        }

        protected override void SetupPositionalArguments(object[] args)
        {
            localarg_a = ((EnvObjectReference)args[0]).Get<int>();
            localarg_b = ((EnvObjectReference)args[1]).Get<int>();
        }
    }

    public class TestMethod : DmlDatumProc
    {
        private List<string> execTrace;

        public TestMethod(List<string> execTrace)
        {
            this.execTrace = execTrace;
        }

        public override string[] ArgumentNames()
        {
            return new string[] { "a", "b" };
        }

        public override IDmlProcAttribute[] Attributes()
        {
            return new IDmlProcAttribute[0];
        }

        protected override DmlDatumProcExecutionContext DmlCreate()
        {
            return new TestMethodContext(execTrace);
        }
    }

    public class NestedTestMethod : DmlDatumProc
    {
        private List<string> execTrace;

        public NestedTestMethod(List<string> execTrace)
        {
            this.execTrace = execTrace;
        }

        public override string[] ArgumentNames()
        {
            return new string[] { "a", "b" };
        }

        public override IDmlProcAttribute[] Attributes()
        {
            return new IDmlProcAttribute[0];
        }

        protected override DmlDatumProcExecutionContext DmlCreate()
        {
            return new NestedTestMethodContext(execTrace);
        }
    }

    internal class NestedTestMethodContext : DmlDatumProcExecutionContext
    {
        private List<string> execTrace;

        int currentStep = 0;
        private int localarg_a;
        private int localarg_b;
        private int localvar_0;

        public NestedTestMethodContext(List<string> execTrace)
        {
            this.execTrace = execTrace;
        }

        private bool AlwaysTrue() => true;

        protected override object DoContinue()
        {
            switch (currentStep)
            {
                case 0:
                    IL.Emit.Br("exec_0");
                    break;
                case 1:
                    IL.Emit.Br("exec_1");
                    break;
                default:
                    throw new Exception("Unknown execution origin...");
            }

            IL.MarkLabel("exec_0");
            execTrace.Add("Nested 0");
            localvar_0 = localarg_a + localarg_b;
            currentStep = 1;

            if (AlwaysTrue())
                throw new DeferExecutionException(DatumProcExecutionState.RUNNING);

            IL.MarkLabel("exec_1");
            execTrace.Add("Nested 1");
            localvar_0 *= 10;

            execTrace.Add($"Nested: {localarg_a}, {localarg_b}, {localvar_0}");

            return localvar_0;
        }

        protected override void SetupPositionalArguments(object[] args)
        {
            localarg_a = ((EnvObjectReference)args[0]).Get<int>();
            localarg_b = ((EnvObjectReference)args[1]).Get<int>();
        }
    }




    public class NestedTestMethod2 : DmlDatumProc
    {
        private List<string> execTrace;

        public NestedTestMethod2(List<string> execTrace)
        {
            this.execTrace = execTrace;
        }

        public override string[] ArgumentNames()
        {
            return new string[] { "a" };
        }

        public override IDmlProcAttribute[] Attributes()
        {
            return new IDmlProcAttribute[0];
        }

        protected override DmlDatumProcExecutionContext DmlCreate()
        {
            return new NestedTestMethodContext2(execTrace);
        }
    }

    internal class NestedTestMethodContext2 : DmlDatumProcExecutionContext
    {
        int currentStep = 0;
        private int localarg_a;
        private int localvar_0;
        private List<string> execTrace;
        private bool AlwaysTrue() => true;

        public NestedTestMethodContext2(List<string> execTrace)
        {
            this.execTrace = execTrace;
        }

        protected override object DoContinue()
        {
            switch (currentStep)
            {
                case 0:
                    IL.Emit.Br("exec_0");
                    break;
                case 1:
                    IL.Emit.Br("exec_1");
                    break;
                default:
                    throw new Exception("Unknown execution origin...");
            }

            IL.MarkLabel("exec_0");
            execTrace.Add("Nested2 0");
            localvar_0 = localarg_a * localarg_a;
            currentStep = 1;

            if (AlwaysTrue())
                throw new DeferExecutionException(DatumProcExecutionState.RUNNING);

            IL.MarkLabel("exec_1");
            execTrace.Add("Nested2 1");
            localvar_0 *= 10;

            execTrace.Add($"Nested2: {localarg_a}, {localvar_0}");

            return localvar_0;
        }

        protected override void SetupPositionalArguments(object[] args)
        {
            localarg_a = ((EnvObjectReference)args[0]).Get<int>();
        }
    }

    [Test]
    public void TestDeferMechanics()
    {
        List<string> execTrace = new();
        var ctx = new TestMethod(execTrace).Create().SetupContext(null, null, null, 0, new ProcArgumentList(9, 23));

        while (ctx.State.Type != DatumProcExecutionExecutionStateType.Completed)
        {
            execTrace.Add("State interpret step...");
            try
            {
                ctx.UnmanagedContinue();
            }
            catch (DeferExecutionException e)
            {
                execTrace.Add("Execution deferred...");
            }

        }
        execTrace.Add(ctx.Result.Get<bool>().ToString());

        var expectedResult = new string[]
        {
            "State interpret step...",
            "Top 0",
            "Execution deferred...",
            "State interpret step...",
            "Top 1",
            "Top 2",
            "Nested2 0",
            "Execution deferred...",
            "State interpret step...",
            "Top 2",
            "Nested2 1",
            "Nested2: 162, 262440",
            "Nested 0",
            "Execution deferred...",
            "State interpret step...",
            "Top 2",
            "Nested 1",
            "Nested: 262440, 33, 2624730",
            "Nested 0",
            "Execution deferred...",
            "State interpret step...",
            "Top 2",
            "Nested 1",
            "Nested: 131, 0, 1310",
            "Top: 9, 23, 162, 33, 131, 0, True",
            "True",
        };

        Assert.IsTrue(execTrace.SequenceEqual(expectedResult));
    }
}*/