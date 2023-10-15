# OpenMud


## What is it?

OpenMud is a compiler which enables a user (ranging from developers to creative individuals with little to no development background) to compile their own MUD games. OpenMud aims to be compatible with a large subset of Dream Maker projects, and compiles your code into behaviourally consistent CIL code (.Net Assemblies). Additionally, the project incorporates an orchestration and simulation layer to leverage these compiled binaries and simulate / ultimately execute the Dream Maker worlds. Effectively, OpenMud is a compiler and a game engine.

OpenMud also implements all of the core client and server side logic necessary to run your game, while also leaving the door open for extensive customizations for advanced developers.

## What is the Licensing
The entire project is licensed under AGPL v3. This means that you are free to use the project for any means, but the project must be open-sourced.

## What is the Current State?

OpenMud is still very early in development. There are various Unit Test cases which demonstrate the current scope covered by the compiler.

If you are interested in contributing, please create an issue and mention me.

I plan to create a detailed table to show what is done and what is missing. Generally speaking, the basics of the DreamMaker Language Compiler & Framework are implemented.

* Currently, coverage supports compiling the "A Step Byond" and "Your First Step" introduction projects to BYOND
* Basic networking and server/client side is implemented
* A very important outstanding task is documentation. This will be completed within the next few weeks.

## What is the Roadmap?

The current goals are:

1. Flesh out the OpenMud Framework implementation so that is can fully support executing the "A Step Byond" project
2. Fleshing out the "omd" CLI Application, which manages compiling and debugging your OpenMud projects.
3. Improve project documentation.

## How do you use it?

The correct way to use OpenMud is via the "omd" CLI application, which handles scaffolding, compiling, debugging and running your OpenMud projects. Documentation for this is on its way. In the mean time, the test cases are quite thorough and can be read to better understand the project organization.

Please explore the test cases under DMLTestCase which explores the current features, how they are tested and how they can be used.

Below is a small snippet of code with demonstrates using the Environment abstraction layer (there is also a higher level ECS abstraction layer which is still being fleshed out.)

```
            var dmlCode =
@"
/mob
    proc
        test(a as num)
            return a * a

/mob/sub
    proc
        test(a as num)
            return a + ..(a - 1)
";
            var assembly = SimpleDmlCompiler.Compile(dmlCode);
            var system = ByondEnvironment.CreateFromAssembly(Assembly.LoadFile(assembly));

            var mob = system.CreateAtomic("/mob/sub");
            Assert.IsTrue((int)mob.ExecProc("test", 5, 6, 7, 8) == 21);
```

Below demonstrates using the ECS abstraction layer to invoke a verb.
```
...
...

        private static void Stabalize(TestGame w)
        {
            for (var i = 0; i < 100; i++)
                w.Update(1);
        }

        [Test]
        public void ObjectCreationTest()
        {
            var dmlCode =
@"
/mob/bob
    verb
        sayHello()
            world << ""Hello World""
";
            var entityBuilder = new BaseEntityBuilder();
            var sceneBuilder = new TestSceneBuilder((w) => {
                entityBuilder.CreateAtomic(w.CreateEntity(), "/mob/bob", "test_mob_instance");
            });

            var assembly = Assembly.LoadFile(SimpleDmlCompiler.Compile(dmlCode));

            var g = new TestGame(sceneBuilder, assembly);

            Stabalize(g);

            g.ExecuteVerb("test_mob_instance", "test_mob_instance", "sayHello", new string[] { });

            Stabalize(g);

            Assert.IsTrue(g.WorldMessages.Single() == "Hello World");

            return;

        }
...
...
```


## Can you mix C# code and Dream Maker Language Code?
Yes, you can define classes in C# and expose them to DreamMaker code. The DreamMaker list runtime type is implemented in C#. Please refer to DmlList implementation in the ByondEcs project.

# Where is the Documentation?
The project documentation is a high priority for this project, and will likely be arriving within the next few weeks. In the mean time, please contact me via GitHub issues to discuss the project.