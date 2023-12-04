using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Compiler.DmlPreprocessor;
using OpenMud.Mudpiler.RuntimeEnvironment;

namespace OpenMud.Mudpiler.Tests.EcsTest;

public class ArgumentConstraintsTest
{
	[Test]
    public void TestInList()
    {
        var dmlCode =
            @"
/var/list/test_global = list(1,2,3)

/proc/testproc()
	return 123

/mob
	var n as num
	verb/ex0(a as anything in list(""on"", ""off""))
		return 1

	verb/ex1(a as anything in list(""on"", ""off""))
		return 1

	verb/ex2(a as anything|null in list(""on"", ""off""))
		world << ""Hello""

	verb/ex3(a as mob in list(src))
		world << a

	verb/ex3(a as mob in list(src.n + 1))
		world << a

	verb/ex3(a as mob in list(n + 1))
		world << a

	verb/ex4(a as mob in clients)
		world << a

	verb/ex5(a as mob in world)
		world << a

	verb/ex5(a[])
		world << a

	verb/ex5(a[])
		world << a

	verb/ex5(a in test_global)
		world << a

	verb/ex5(a in testproc())
		world << a
";

        dmlCode = Preprocessor.Preprocess("testfile.dme", ".", dmlCode, (a, b) => throw new NotImplementedException(),
            (a, b, c, d) => throw new Exception("Import not supported"), EnvironmentConstants.BUILD_MACROS);

        var assembly = MsBuildDmlCompiler.Compile(dmlCode);
    }
}