using Cake.Common;
using Cake.Common.Solution;
using Cake.Common.IO;
using Cake.Common.Tools.MSBuild;
using Cake.Common.Tools.NuGet;
using Cake.Core;
using Cake.Common.Diagnostics;
using SimpleGitVersion;
using Code.Cake;
using Cake.Common.Build.AppVeyor;
using Cake.Common.Tools.NuGet.Pack;
using System;
using System.Linq;
using Cake.Common.Tools.SignTool;
using Cake.Core.Diagnostics;
using Cake.Common.Text;
using Cake.Common.Tools.NuGet.Push;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Cake.Common.Tools.NUnit;
using Cake.Common.Tools.DotNetCore;
using Cake.Core.IO;
using Cake.Common.Tools.DotNetCore.Pack;
using Cake.Common.Build;
using Cake.Common.Tools.DotNetCore.Test;
using CK.Text;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Common.Tools.DotNetCore.Restore;

namespace CodeCake
{

    /// <summary>
    /// Standard build "script".
    /// </summary>
    [AddPath( "%UserProfile%/.nuget/packages/**/tools*" )]
    public partial class Build : CodeCakeHost
    {
        public Build()
        {
            Cake.Log.Verbosity = Verbosity.Diagnostic;

            const string solutionName = "CK-Setup-Dependency";
            const string solutionFileName = solutionName + ".sln";

            var releasesDir = Cake.Directory( "CodeCakeBuilder/Releases" );

            var projects = Cake.ParseSolution( solutionFileName )
                           .Projects
                           .Where( p => !(p is SolutionFolder)
                                        && p.Name != "CodeCakeBuilder" );

            // We do not publish .Tests projects for this solution.
            var projectsToPublish = projects
                                        .Where( p => !p.Path.Segments.Contains( "Tests" ) );

            SimpleRepositoryInfo gitInfo = Cake.GetSimpleRepositoryInfo();

            // Configuration is either "Debug" or "Release".
            string configuration = "Debug";

            Task( "Check-Repository" )
                .Does( () =>
                 {
                     configuration = StandardCheckRepository( projectsToPublish, gitInfo );
                 } );

            Task( "Clean" )
                .IsDependentOn( "Check-Repository" )
                .Does( () =>
                 {
                     Cake.CleanDirectories( projects.Select( p => p.Path.GetDirectory().Combine( "bin" ) ) );
                     Cake.CleanDirectories( releasesDir );
                     Cake.DeleteFiles( "Tests/**/TestResult*.xml" );
                 } );

            Task( "Build" )
                .IsDependentOn( "Check-Repository" )
                .IsDependentOn( "Clean" )
                .Does( () =>
                 {
                     StandardSolutionBuild( solutionFileName, gitInfo, configuration );
                 } );

            Task( "Unit-Testing" )
                .IsDependentOn( "Build" )
                .Does( () =>
                 {
                     StandardUnitTests( configuration, projects.Where( p => p.Name.EndsWith( ".Tests" ) ) );
                 } );

            Task( "Create-NuGet-Packages" )
                .WithCriteria( () => gitInfo.IsValid )
                .IsDependentOn( "Unit-Testing" )
                .Does( () =>
                 {
                     StandardCreateNuGetPackages( releasesDir, projectsToPublish, gitInfo, configuration );
                 } );


            Task( "Push-NuGet-Packages" )
                .IsDependentOn( "Create-NuGet-Packages" )
                .WithCriteria( () => gitInfo.IsValid )
                .Does( () =>
                 {
                     IEnumerable<FilePath> nugetPackages = Cake.GetFiles( releasesDir.Path + "/*.nupkg" );
                     StandardPushNuGetPackages( nugetPackages, gitInfo );
                 } );

            // The Default task for this script can be set here.
            Task( "Default" )
                .IsDependentOn( "Push-NuGet-Packages" );

        }
    }
}
