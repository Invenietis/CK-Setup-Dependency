using NUnit.Common;
using NUnitLite;
using System;
using System.Reflection;

namespace CK.Setup.Dependency.Tests
{
    public class Program
    {
        public static int Main( string[] args )
        {
            var r = new AutoRun( typeof( Program ).GetTypeInfo().Assembly )
                .Execute( args, new ExtendedTextWrapper( Console.Out ), Console.In );
            Console.ReadLine();
            return r;
        }

    }
}
