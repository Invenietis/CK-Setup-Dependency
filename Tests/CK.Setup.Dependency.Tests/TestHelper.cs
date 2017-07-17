using System.IO;
using NUnit.Framework;
using CK.Core;
using System;
using System.Linq;
using System.Diagnostics;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using CK.Text;
using System.Reflection;
using NUnit.Framework.Constraints;
using System.Collections.Generic;

namespace CK.Setup.Dependency.Tests
{
#if NET461
    public static class Does
    {
        public static SubstringConstraint Contain( string expected ) => Is.StringContaining( expected );

        public static EndsWithConstraint EndWith( string expected ) => Is.StringEnding( expected );

        public static StartsWithConstraint StartWith( string expected ) => Is.StringStarting( expected );

        public static ConstraintExpression Not => Is.Not;

        public static SubstringConstraint Contain( this ConstraintExpression @this, string expected ) => @this.StringContaining( expected );
    }
#endif

    public static class TestHelper
    {
        static string _solutionFolder;
        static string _configuration;

        static IActivityMonitor _monitor;
        static ActivityMonitorConsoleClient _console;

        static TestHelper()
        {
            _monitor = new ActivityMonitor();
            _monitor.Output.BridgeTarget.HonorMonitorFilter = false;
            _console = new ActivityMonitorConsoleClient();
        }

        public static IActivityMonitor ConsoleMonitor => _monitor;

        public static bool LogsToConsole
        {
            get { return _monitor.Output.Clients.Contains( _console ); }
            set
            {
                if( value != LogsToConsole )
                {
                    if( value )
                    {
                        _monitor.Output.RegisterUniqueClient( c => c == _console, () => _console );
                        _monitor.Info( "Enabled Logs to console." );
                    }
                    else
                    {
                        _monitor.Info( "Disabled Logs to console." );
                        _monitor.Output.UnregisterClient( _console );
                    }
                }
            }
        }

        public static string SolutionFolder
        {
            get
            {
                if( _solutionFolder == null ) InitalizePaths();
                return _solutionFolder;
            }
        }

        public static string Configuration
        {
            get
            {
                if( _solutionFolder == null ) InitalizePaths();
                return _configuration;
            }
        }

        public static string CurrentTestProjectName => "CK.Setup.Dependency.Tests";

        public static string BuildPathInCurrentTestProject( params string[] subNames )
        {
            var all = new List<string>();
            all.Add( SolutionFolder );
            all.Add( "Tests" );
            all.Add( CurrentTestProjectName );
            all.AddRangeArray( subNames );
            return Path.Combine( all.ToArray() );
        }

        #region Trace for IDependentItem

        public static void Trace( IEnumerable<IDependentItem> e )
        {
            foreach( var i in e ) Trace( i );
        }

        public static void Trace( IDependentItem i )
        {
            using( _monitor.OpenTrace( $"FullName = {i.FullName}" ) )
            {
                _monitor.Trace( $"Container = {OneName( i.Container )}" );
                _monitor.Trace( $"Generalization = {OneName( i.Generalization )}" );
                _monitor.Trace( $"Requires = {Names( i.Requires )}" );
                _monitor.Trace( $"RequiredBy = {Names( i.RequiredBy )}" );
                _monitor.Trace( $"Groups = {Names( i.Groups )}" );
                IDependentItemGroup g = i as IDependentItemGroup;
                if( g != null )
                {
                    IDependentItemContainerTyped c = i as IDependentItemContainerTyped;
                    if( c != null )
                    {
                        _monitor.Trace( $"[{c.ItemKind.ToString()[0]}]Children = {Names( g.Children )}" );
                    }
                    else _monitor.Trace( $"[G]Children = {Names( g.Children )}" );
                }
            }
        }

        static string Names( IEnumerable<IDependentItemRef> ee )
        {
            return ee != null ? String.Join( ", ", ee.Select( o => OneName( o ) ) ) : String.Empty;
        }

        static string OneName( IDependentItemRef o )
        {
            return o != null ? o.FullName + " (" + o.GetType().Name + ")" : "(null)";
        }

        #endregion 

        #region Trace for ISortedItem

        public static void Trace( IEnumerable<ISortedItem> e, bool skipGroupTail )
        {
            foreach( var i in e )
                if( i.HeadForGroup == null || skipGroupTail )
                    Trace( i );
        }

        public static void Trace( ISortedItem i )
        {
            using( _monitor.OpenTrace( $"[{i.FullName}]FullName = {i.ItemKind.ToString()[0]}" ) )
            {
                _monitor.Trace( $"Container = {(i.Container != null ? i.Container.FullName : "(null)")}" );
                _monitor.Trace( $"(Generalization = {(i.Generalization != null ? i.Generalization.FullName : "(null)")}" );
                _monitor.Trace( $"Requires = {Names( i.Requires )}" );
                _monitor.Trace( $"Groups = {Names( i.Groups )}" );
                _monitor.Trace( $"Children = {Names( i.Children )}" );
            }
        }

        static string Names( IEnumerable<ISortedItem> ee )
        {
            return ee != null ? String.Join( ", ", ee.Select( o => o.FullName ) ) : String.Empty;
        }
        #endregion


        static void InitalizePaths()
        {
#if DEBUG
            _configuration = "Debug";
#else
            _configuration = "Release";
#endif
            string p = AppContext.BaseDirectory;
            while( !Directory.EnumerateFiles( p ).Where( f => f.EndsWith( ".sln" ) ).Any() )
            {
                p = Path.GetDirectoryName( p );
            }
            _solutionFolder = p;

            Console.WriteLine( $"SolutionFolder is: {_solutionFolder}." );
            Console.WriteLine( $"Core path: {typeof( string ).GetTypeInfo().Assembly.CodeBase}." );
        }

    }
}
