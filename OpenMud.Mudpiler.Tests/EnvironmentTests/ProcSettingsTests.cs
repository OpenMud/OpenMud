using OpenMud.Mudpiler.Compiler.Core;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.RuntimeEnvironment;
using OpenMud.Mudpiler.RuntimeEnvironment.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenMud.Mudpiler.Tests.EnvironmentTests
{
    public class ProcSettingsTests
    {
        [Test]
        public void TestProcBackgroundAttribute()
        {
            var dmlCode =
                @"
/mob/test
    proc/testa_a()
        set background = 1
        return 0

    proc/testa_b()
        set background = 1
        return 0
";
            var assembly = MsBuildDmlCompiler.Compile(dmlCode);
            var system = MudEnvironment.Create(Assembly.LoadFile(assembly), new BaseDmlFramework());
            var asm = system.CreateAtomic("/mob/test");

            var atm = asm.Unwrap<RuntimeEnvironment.WorldPiece.Atom>();

            var proc = atm.GetProc("testa_a");
            var attributes = proc.Attributes().Where(x => x is BackgroundProcessing).Cast<BackgroundProcessing>();
            Assert.IsTrue(attributes.Single().Background == true);

            proc = atm.GetProc("testa_b");
            attributes = proc.Attributes().Where(x => x is BackgroundProcessing).Cast<BackgroundProcessing>();
            Assert.IsTrue(attributes.Count() == 0 || attributes.Single().Background == true);
        }
    }
}
