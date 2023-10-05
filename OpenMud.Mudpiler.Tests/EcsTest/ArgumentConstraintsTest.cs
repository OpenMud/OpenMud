namespace OpenMud.Mudpiler.Tests.EcsTest;

public class ArgumentConstraintsTest
{
    public void TestInList()
    {
        var dmlCode =
            @"
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
";
        //ex0, ex1: Test that accepts on, off, but nothing else
        //ex2: Allow argument to be omitted.
        //ex3: only allow mobs of self as argument.
        Assert.IsTrue(false);
    }
}