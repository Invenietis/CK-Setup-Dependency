#if !NET451
using NUnit.Common;
using NUnitLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace CK.Setup.Dependency.Tests
{
    public static class StandardMain
    {
        public static int StdMain(Type programType, string[] args)
        {
            int idxGui = HandleArgument(ref args, "-gui");
            if (idxGui >= 0)
            {
                var nunit = Path.Combine(TestHelper.SolutionFolder, "packages", "NUnit.Runners.Net4.2.6.4", "tools", "nunit.exe");
                var toTest = Path.Combine(Directory.GetCurrentDirectory(), "bin", TestHelper.Configuration, "net451", TestHelper.CurrentTestProjectName + ".exe");
                var p = Process.Start(nunit, "\"" + toTest + "\" " + string.Join(" ", args));
                return 0;
            }
            int idxPause = HandleArgument(ref args, "-pause");
            int result = new AutoRun(programType.GetTypeInfo().Assembly)
                .Execute(args, new ExtendedTextWrapper(Console.Out), Console.In);
            if (idxPause >= 0)
            {
                Console.Write("Hit a key.");
                Console.ReadKey();
            }
            return result;
        }

        private static int HandleArgument(ref string[] args, string argument)
        {
            int idxPause = Array.IndexOf(args, argument);
            if (idxPause >= 0)
            {
                var l = new List<string>(args);
                l.RemoveAt(idxPause);
                args = l.ToArray();
            }

            return idxPause;
        }
    }
}
#else
using System;
using System.Diagnostics;

namespace CK.Setup.Dependency.Tests
{
    public static class StandardMain
    {
        public static int StdMain(Type main, string[] args)
        {
            return 0;
        }

    }
}
#endif