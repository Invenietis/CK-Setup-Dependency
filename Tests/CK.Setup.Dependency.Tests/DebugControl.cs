using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CK.Setup.Dependency
{
    [TestFixture]
    public class DebugControl
    {
        [Explicit]
        [Test]
        public void DebuggerLaunch()
        {
            Debugger.Launch();
        }

    }
}
