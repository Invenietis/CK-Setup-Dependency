#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\ContainerDependencies.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CK.Core;
using static CK.Testing.MonitorTestHelper;
using Shouldly;

namespace CK.Setup.Dependency.Tests;

[TestFixture]
public class ContainerDependencies
{
    [Test]
    public void an_empty_container_has_its_head_before_its_content()
    {
        var c = new TestableContainer( "C" );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c );
            r.AssertOrdered( "C.Head", "C" );
            ResultChecker.SimpleCheckAndReset( r );
        }
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, true, c );
            r.AssertOrdered( "C.Head", "C" );
            ResultChecker.SimpleCheckAndReset( r );
        }
    }

    [Test]
    public void container_children_are_automatically_registered()
    {
        var c = new TestableContainer( "C", new TestableItem( "A" ), new TestableItem( "B" ) );

        var r = DependencySorter.OrderItems( TestHelper.Monitor, c );
        Throw.Assert( r.IsComplete );
        r.SortedItems.Count.ShouldBe(4);

        r.SortedItems[0].IsGroupHead.ShouldBeTrue( "Head of Container." );
        r.SortedItems[1].Item.FullName.ShouldBe( "A", "Lexical order." );
        r.SortedItems[2].Item.FullName.ShouldBe( "B", "Lexical order." );
        r.SortedItems[3].Item.FullName.ShouldBe( "C", "Container" );
        new ResultChecker( r ).CheckRecurse( c.FullName );
        ResultChecker.SimpleCheckAndReset( r );
        r.CheckChildren( "C", "A,B" );
    }

    [Test]
    public void a_container_is_automatically_registered_by_any_of_its_children()
    {
        var c = new TestableContainer( "ZeContainer", new TestableItem( "A" ), new TestableItem( "B" ) );
        var e = new TestableItem( "E" );
        c.Add( e );

        var r = DependencySorter.OrderItems( TestHelper.Monitor, e );
        Throw.Assert( r.IsComplete );
        r.SortedItems.Count.ShouldBe(5);

        r.SortedItems[0].IsGroupHead.ShouldBeTrue( "Head of Container." );
        r.SortedItems[1].Item.FullName.ShouldBe( "A", "Lexical order." );
        r.SortedItems[2].Item.FullName.ShouldBe( "B", "Lexical order." );
        r.SortedItems[3].Item.FullName.ShouldBe( "E", "Lexical order." );
        r.SortedItems[4].Item.FullName.ShouldBe( "ZeContainer", "Container" );

        new ResultChecker( r ).CheckRecurse( c.FullName, e.FullName );
        ResultChecker.SimpleCheckAndReset( r );
        r.CheckChildren( "ZeContainer", "A,B,E" );
    }

    [Test]
    public void one_package_with_its_model()
    {
        var pA = new TestableContainer( "A" );
        var pAModel = new TestableContainer( "Model.A" );

        Action test = () =>
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, pAModel, pA );
                ResultChecker.SimpleCheckAndReset( r );
                var rRevert = DependencySorter.OrderItems( TestHelper.Monitor, true, pAModel, pA );
                ResultChecker.SimpleCheckAndReset( rRevert );

                r.AssertOrdered( "Model.A.Head", "Model.A", "A.Head", "A" );
                rRevert.AssertOrdered( "Model.A.Head", "Model.A", "A.Head", "A" );
            };

        pA.Requires.Add( pAModel );
        test();

        pA.Requires.Clear();
        pAModel.RequiredBy.Add( pA );
        test();
    }

    [Test]
    public void packages_with_model()
    {
        var pA = new TestableContainer( "A" );
        var pAModel = new TestableContainer( "Model.A" );
        var pB = new TestableContainer( "B" );
        var pBModel = new TestableContainer( "Model.B" );

        Action testAndRestore = () =>
            {
                var r1 = DependencySorter.OrderItems( TestHelper.Monitor, pAModel, pA, pBModel, pB );
                ResultChecker.SimpleCheckAndReset( r1 );
                var r2 = DependencySorter.OrderItems( TestHelper.Monitor, true, pAModel, pA, pBModel, pB );
                ResultChecker.SimpleCheckAndReset( r2 );

                // There is no constraint between A and Model.B: depending on the sort order this changes.
                r1.AssertOrdered( "Model.A.Head", "Model.A", "A.Head", "Model.B.Head", "A", "Model.B", "B.Head", "B" );
                r2.AssertOrdered( "Model.A.Head", "Model.A", "Model.B.Head", "A.Head", "Model.B", "A", "B.Head", "B" );

                pB.RequiredBy.Clear();
                pA.RequiredBy.Clear();
                pAModel.RequiredBy.Clear();
                pBModel.RequiredBy.Clear();
                pB.Requires.Clear();
                pA.Requires.Clear();
                pAModel.Requires.Clear();
                pBModel.Requires.Clear();
            };

        pB.Requires.Add( pA );
        pB.Requires.Add( pBModel );
        pA.Requires.Add( pAModel );
        pBModel.Requires.Add( pAModel );
        testAndRestore();

        pA.RequiredBy.Add( pB );
        pBModel.RequiredBy.Add( pB );
        pAModel.RequiredBy.Add( pA );
        pAModel.RequiredBy.Add( pBModel );
        testAndRestore();
    }

    [TestCase( true, true )]
    [TestCase( false, true )]
    [TestCase( true, false )]
    [TestCase( false, false )]
    public void Requires_and_RequiredBy_on_containers( bool reverseName, bool revertReg )
    {
        var pA = new TestableContainer( "A", "↽B" );
        var pB = new TestableContainer( "B" );
        var pC = new TestableContainer( "C", "⇀B", "↽E" ); // E is duplicate RequiredBy. 
        var pD = new TestableContainer( "D", "⇀C", "↽E", "⇀B" ); // B is duplicate Requires.
        var pE = new TestableContainer( "E" );

        IEnumerable<TestableItem> reg = [pA, pB, pC, pD, pE];
        if( revertReg ) reg = reg.Reverse();
        var r = DependencySorter.OrderItems( TestHelper.Monitor, reg, discoverers: null, new DependencySorterOptions { ReverseName = reverseName } );
        r.AssertOrdered( "A.Head", "A", "B.Head", "B", "C.Head", "C", "D.Head", "D", "E.Head", "E" );
        Throw.DebugAssert( r.SortedItems != null );
        r.SortedItems.Single( s => s.FullName == "A" ).Requires.ShouldBeEmpty();
        r.SortedItems.Single( s => s.FullName == "B" ).Requires.Select( r => r.FullName ).ShouldBe( ["A"] );
        r.SortedItems.Single( s => s.FullName == "C" ).Requires.Select( r => r.FullName ).ShouldBe( ["B"] );
        r.SortedItems.Single( s => s.FullName == "D" ).Requires.Select( r => r.FullName ).ShouldBe( ["C"] );
        r.SortedItems.Single( s => s.FullName == "E" ).Requires.Select( r => r.FullName ).ShouldBe( ["D"] );
    }

    [TestCase( true, true )]
    [TestCase( false, true )]
    [TestCase( true, false )]
    [TestCase( false, false )]
    public void Requires_and_RequiredBy_on_items( bool reverseName, bool revertReg )
    {
        var pA = new TestableItem( "A", "↽B" );
        var pB = new TestableItem( "B" );
        var pC = new TestableItem( "C", "⇀B", "↽E" ); // E is duplicate RequiredBy.
        var pD = new TestableItem( "D", "⇀C", "↽E", "⇀B" ); // B is duplicate Requires.
        var pE = new TestableItem( "E" );

        IEnumerable<TestableItem> reg = [pA, pB, pC, pD, pE];
        if( revertReg ) reg = reg.Reverse();
        var r = DependencySorter.OrderItems( TestHelper.Monitor, reg, discoverers: null, new DependencySorterOptions { ReverseName = reverseName } );
        r.AssertOrdered( "A", "B", "C", "D", "E" );
        r.SortedItems.Single( s => s.FullName == "A" ).Requires.Select( r => r.FullName ).ShouldBeEmpty();
        r.SortedItems.Single( s => s.FullName == "B" ).Requires.Select( r => r.FullName ).ShouldBe( ["A"] );
        r.SortedItems.Single( s => s.FullName == "C" ).Requires.Select( r => r.FullName ).ShouldBe( ["B"] );
        r.SortedItems.Single( s => s.FullName == "D" ).Requires.Select( r => r.FullName ).ShouldBe( ["C"] );
        r.SortedItems.Single( s => s.FullName == "E" ).Requires.Select( r => r.FullName ).ShouldBe( ["D"] );
    }

    [TestCase( true, true )]
    [TestCase( false, true )]
    [TestCase( true, false )]
    [TestCase( false, false )]
    public void Requires_and_RequiredBy_on_mix( bool reverseName, bool revertReg )
    {
        var pA = new TestableItem( "A", "↽B" );
        var pB = new TestableContainer( "B" );
        var pC = new TestableItem( "C", "⇀B", "↽E" ); // E is duplicate RequiredBy.
        var pD = new TestableContainer( "D", "⇀C", "↽E", "⇀B" ); // B is duplicate Requires.
        var pE = new TestableItem( "E" );

        IEnumerable<TestableItem> reg = [pA, pB, pC, pD, pE];
        if( revertReg ) reg = reg.Reverse();
        var r = DependencySorter.OrderItems( TestHelper.Monitor, reg, discoverers: null, new DependencySorterOptions { ReverseName = reverseName } );
        r.SortedItems.Where( s => !s.IsGroupHead ).Select( s => s.FullName ).Concatenate().ShouldBe( "A, B, C, D, E" );
        GetSorted( "A" ).Requires.Select( r => r.FullName ).ShouldBeEmpty();
        GetSorted( "B" ).Requires.Select( r => r.FullName ).ShouldBe( ["A"] );
        GetSorted( "C" ).Requires.Select( r => r.FullName ).ShouldBe( ["B"] );
        GetSorted( "D" ).Requires.Select( r => r.FullName ).ShouldBe( ["C"] );
        GetSorted( "E" ).Requires.Select( r => r.FullName ).ShouldBe( ["D"] );

        ISortedItem GetSorted( string name )
        {
            return r.SortedItems.Single( s => s.FullName == name );
        }

    }

    [Test]
    public void packages_with_model_and_objects()
    {
        var pA = new TestableContainer( "A" );
        var pAModel = new TestableContainer( "Model.A" );
        var pAObjects = new TestableContainer( "Objects.A" );
        var pB = new TestableContainer( "B" );
        var pBModel = new TestableContainer( "Model.B" );
        var pBObjects = new TestableContainer( "Objects.B" );
        var all = new[] { pA, pAModel, pAObjects, pB, pBModel, pBObjects };

        Action testAndRestore = () =>
        {
            var r1 = DependencySorter.OrderItems( TestHelper.Monitor, all );
            ResultChecker.SimpleCheckAndReset( r1 );
            var r2 = DependencySorter.OrderItems( TestHelper.Monitor, true, all );
            ResultChecker.SimpleCheckAndReset( r2 );

            // There is no constraint between:
            // - A and Model.B
            // - A and Objects.B
            // depending on the sort order this changes.
            r1.AssertOrdered( "Model.A.Head", "Model.A", "A.Head", "Model.B.Head", "A", "Model.B", "B.Head", "Objects.A.Head", "B", "Objects.A", "Objects.B.Head", "Objects.B" );
            r2.AssertOrdered( "Model.A.Head", "Model.A", "Model.B.Head", "A.Head", "Model.B", "A", "Objects.A.Head", "B.Head", "Objects.A", "B", "Objects.B.Head", "Objects.B" );

            foreach( var p in all )
            {
                p.RequiredBy.Clear();
                p.Requires.Clear();
            }
        };

        pB.Requires.Add( pA );
        pB.Requires.Add( pBModel );
        pA.Requires.Add( pAModel );
        pBModel.Requires.Add( pAModel );
        pBObjects.Requires.Add( pB );
        pAObjects.Requires.Add( pA );
        pBObjects.Requires.Add( pAObjects );
        testAndRestore();

        Random r = new Random();
        for( int i = 0; i < 30; ++i )
        {
            if( r.Next( 2 ) == 0 ) pB.Requires.Add( pA ); else pA.RequiredBy.Add( pB );
            if( r.Next( 2 ) == 0 ) pB.Requires.Add( pBModel ); else pBModel.RequiredBy.Add( pB );
            if( r.Next( 2 ) == 0 ) pA.Requires.Add( pAModel ); else pAModel.RequiredBy.Add( pA );
            if( r.Next( 2 ) == 0 ) pBModel.Requires.Add( pAModel ); else pAModel.RequiredBy.Add( pBModel );
            if( r.Next( 2 ) == 0 ) pBObjects.Requires.Add( pB ); else pB.RequiredBy.Add( pBObjects );
            if( r.Next( 2 ) == 0 ) pAObjects.Requires.Add( pA ); else pA.RequiredBy.Add( pAObjects );
            if( r.Next( 2 ) == 0 ) pBObjects.Requires.Add( pAObjects ); else pAObjects.RequiredBy.Add( pBObjects );
        }
    }

    [Test]
    public void registering_order_does_not_matter()
    {
        var c0 = new TestableContainer( "C0", new TestableItem( "A" ), new TestableItem( "B" ), new TestableItem( "C" ) );
        var c1 = new TestableContainer( "C1", new TestableItem( "X" ), "⇀C0" );
        var c2 = new TestableContainer( "C2", new TestableItem( "Y" ), "⇀C1" );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c2, c0, c1 );
            r.AssertOrdered( "C0.Head", "A", "B", "C", "C0", "C1.Head", "X", "C1", "C2.Head", "Y", "C2" );
            new ResultChecker( r ).CheckRecurse( "C0", "C1", "C2" );
            ResultChecker.SimpleCheckAndReset( r );
            r.CheckChildren( "C0", "A,B,C" );
        }
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c0, c1, c2 );
            r.AssertOrdered( "C0.Head", "A", "B", "C", "C0", "C1.Head", "X", "C1", "C2.Head", "Y", "C2" );
            new ResultChecker( r ).CheckRecurse( "C0", "C1", "C2" );
            ResultChecker.SimpleCheckAndReset( r );
            r.CheckChildren( "C0", "A,B,C" );
        }
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c2, c1, c0 );
            r.AssertOrdered( "C0.Head", "A", "B", "C", "C0", "C1.Head", "X", "C1", "C2.Head", "Y", "C2" );
            new ResultChecker( r ).CheckRecurse( "C0", "C1", "C2" );
            ResultChecker.SimpleCheckAndReset( r );
            r.CheckChildren( "C0", "A,B,C" );
        }
    }

    [Test]
    public void ItemToContainer()
    {
        var c0 = new TestableContainer( "C0", new TestableItem( "A" ), new TestableItem( "B" ), new TestableItem( "C" ) );
        var c1 = new TestableContainer( "C1", new TestableItem( "X", "⇀C0" ) );
        var c2 = new TestableContainer( "C2", new TestableItem( "Y", "⇀C1" ) );
        var r = DependencySorter.OrderItems( TestHelper.Monitor, c2, c0, c1 );
        new ResultChecker( r ).CheckRecurse( "C0", "C1", "C2" );
        ResultChecker.SimpleCheckAndReset( r );
        r.CheckChildren( "C0", "A,B,C" );
        r.CheckChildren( "C1", "X" );
        r.CheckChildren( "C2", "Y" );
    }


    [Test]
    public void missing_dependencies()
    {
        var c = new TestableContainer( "Root", "⇀Direct",
                    new TestableContainer( "Pierre", "↽?Direct", "↽Direct",
                        new TestableItem( "Rubis" )
                        ),
                    new TestableContainer( "Nuage", "⇀?OptDirect", "⇀?OptDirect",
                        new TestableItem( "Cumulus" ),
                        new TestableItem( "Stratus" )
                        )
            );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c );
            r.IsComplete.ShouldBeFalse();
            r.ItemIssues[0].Item.FullName.ShouldBe("Root");
            r.ItemIssues[0].RequiredMissingCount.ShouldBe(1);
            r.ItemIssues[0].MissingDependencies.Count().ShouldBe(1);
            r.ItemIssues[0].MissingDependencies.First().ShouldBe("Direct");

            r.ItemIssues[1].Item.FullName.ShouldBe("Nuage");
            r.ItemIssues[1].RequiredMissingCount.ShouldBe(0);
            r.ItemIssues[1].MissingDependencies.Count().ShouldBe(1);
            r.ItemIssues[1].MissingDependencies.First().ShouldBe("?OptDirect");

            ResultChecker.SimpleCheckAndReset( r );
        }
    }

    [Test]
    public void Cycle_detection_0()
    {
        var c = new TestableContainer( "A", "⇀ A" );
        var r = DependencySorter.OrderItems( TestHelper.Monitor, c );
        r.CycleDetected.ShouldNotBeNull();
        r.CycleExplainedString.ShouldBe("↳ A ⇀ A");
        ResultChecker.SimpleCheckAndReset( r );
    }

    [Test]
    public void Cycle_detection_1()
    {
        var c = new TestableContainer( "Root",
                    new TestableContainer( "Pierre", "⇀Stratus",
                        new TestableItem( "Rubis" )
                        ),
                    new TestableContainer( "Nuage", "⇀Pierre",
                        new TestableItem( "Cumulus" ),
                        new TestableItem( "Stratus" )
                        )
            );
        var r = DependencySorter.OrderItems( TestHelper.Monitor, c );
        r.CycleDetected.ShouldNotBeNull();
        r.SortedItems.ShouldBeNull();
        // The detected cycle depends on the algorithm. 
        // This works here because since we register the Root, the last registered child is Nuage: we know
        // that the cycle starts (and ends) with Nuage because children are in linked list (added at the head).
        // (This remarks is valid for the other CycleDetection below.)
        r.CycleExplainedString.ShouldBe("↳ Nuage ⇀ Pierre ⇀ Stratus ⊏ Nuage");
        ResultChecker.SimpleCheckAndReset( r );
    }

    [Test]
    public void Cycle_detection_2()
    {
        var c = new TestableContainer( "Root",
                    new TestableContainer( "Pierre",
                        new TestableItem( "Rubis", "⇀Stratus" )
                        ),
                    new TestableContainer( "Nuage", "⇀Pierre",
                        new TestableItem( "Cumulus" ),
                        new TestableItem( "Stratus" )
                        )
            );
        var r = DependencySorter.OrderItems( TestHelper.Monitor, c );
        r.CycleDetected.ShouldNotBeNull();
        r.SortedItems.ShouldBeNull();
        // See remark in CycleDetection1.
        r.CycleExplainedString.ShouldBe("↳ Nuage ⇀ Pierre ⊐ Rubis ⇀ Stratus ⊏ Nuage");
        ResultChecker.SimpleCheckAndReset( r );
    }

    [Test]
    public void Cycle_detection_3()
    {
        var c = new TestableContainer( "Root",
                    new TestableContainer( "Pierre",
                        new TestableItem( "Rubis" )
                        ),
                    new TestableContainer( "Nuage", "⇀Pierre",
                        new TestableItem( "Cumulus" ),
                        new TestableItem( "Stratus", "↽ Rubis" )
                        )
            );
        var r = DependencySorter.OrderItems( TestHelper.Monitor, c );
        r.CycleDetected.ShouldNotBeNull();
        r.SortedItems.ShouldBeNull();
        // See remark in CycleDetection1.
        // Here we can see the RequiredByRequires relation: ⇌
        r.CycleExplainedString.ShouldBe("↳ Nuage ⇀ Pierre ⊐ Rubis ⇌ Stratus ⊏ Nuage");
        ResultChecker.SimpleCheckAndReset( r );
    }

    [Test]
    public void Wahoo()
    {
        var c = new TestableContainer( "Root",
            new TestableItem( "A", "⇀B" ),
            new TestableItem( "B" ),
            new TestableContainer( "G1",
                new TestableItem( "C", "⇀ AMissingDependency", "⇀E" ),
                new TestableContainer( "G1.0", "⇀ A",
                    new TestableItem( "NeedInZ", "⇀InsideZ" )
                    )
                ),
            new TestableContainer( "Z", "⇀E",
                new TestableItem( "InsideZ", "⇀C", "⇀ ?OptionalMissingDep" )
                ),
            new TestableItem( "E", "⇀B" ),
            new TestableContainer( "Pierre",
                new TestableItem( "Rubis" )
                ),
            new TestableContainer( "Nuage", "⇀Pierre",
                new TestableItem( "Cumulus", "↽ RequiredByAreIgnoredIfMissing", "↽ ?IfMarkedAsOptinalTheyContinueToBeIgnored" ),
                new TestableItem( "Stratus" )
                )
            );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c );
            Throw.Assert( r.ItemIssues.Any( m => m.MissingDependencies.Contains( "AMissingDependency" ) ) );
            new ResultChecker( r ).CheckRecurse( "Root" );
            ResultChecker.SimpleCheckAndReset( r );
        }
        {
            // Ordering handles duplicates.
            var r = DependencySorter.OrderItems( TestHelper.Monitor, new IDependentItem[] { c, c } );
            Throw.Assert( r.ItemIssues.Any( m => m.MissingDependencies.Contains( "AMissingDependency" ) ) );
            new ResultChecker( r ).CheckRecurse( "Root" );
            ResultChecker.SimpleCheckAndReset( r );
        }
        {
            // Ordering handles duplicates.
            var r = DependencySorter.OrderItems( TestHelper.Monitor, new IDependentItem[] { c, c }.Concat( c.Children.Cast<IDependentItem>() ).Concat( c.Children.Cast<IDependentItem>() ), null );
            Throw.Assert( r.ItemIssues.Any( m => m.MissingDependencies.Contains( "AMissingDependency" ) ) );
            new ResultChecker( r ).CheckRecurse( "Root" );
            ResultChecker.SimpleCheckAndReset( r );
        }
    }

    [Test]
    public void Simple_graph()
    {
        var pAB = new TestableContainer( "PackageForAB" );
        var oA = new TestableItem( "A" );
        oA.Container = pAB;
        var oB = new TestableItem( "B" );
        oB.Container = pAB;
        oB.Requires.Add( oA );
        var pABLevel1 = new TestableContainer( "PackageForABLevel1" );
        pABLevel1.Requires.Add( pAB );
        var oBLevel1 = new TestableItem( "ObjectBLevel1" );
        oBLevel1.Container = pABLevel1;
        oBLevel1.Requires.Add( oA );

        var r = DependencySorter.OrderItems( TestHelper.Monitor, pAB, oA, oB, pABLevel1, oBLevel1 );
        Throw.Assert( r.IsComplete );
        r.AssertOrdered( "PackageForAB.Head", "A", "B", "PackageForAB", "PackageForABLevel1.Head", "ObjectBLevel1", "PackageForABLevel1" );
        r.CheckChildren( "PackageForAB", "A,B" );
        r.CheckChildren( "PackageForABLevel1", "ObjectBLevel1" );
    }

    [Test]
    public void Cofely_Feedermarket_solutions_and_projects()
    {
        var fmBuildings = new TestableContainer( "Feedermarket-Buildings.sln" );
        {
            var fmBuildProj = new TestableItem( "Feedermarket.Buildings" )
            {
                Container = fmBuildings
            };
            fmBuildings.Children.Add( fmBuildProj );
        }
        var fmBuildPckg = new TestableItem( "NuGet:Feedermarket.Buildings" );
        fmBuildPckg.Requires.Add( fmBuildings );

        var cofely = new TestableContainer( "Cofely.sln" );
        {
            var cflyTarget = new TestableItem( "CFLY.Target" );
            cflyTarget.Requires.Add( fmBuildPckg );
            cofely.Children.Add( cflyTarget );
            var cflyTargetData = new TestableItem( "CFLY.Target.Data" );
            cflyTargetData.Requires.Add( cflyTarget );
            cofely.Children.Add( cflyTargetData );
            var cflyIntranet = new TestableItem( "CFLY.Intranet" );
            cflyIntranet.Requires.Add( cflyTargetData );
            cofely.Children.Add( cflyIntranet );
            var cflyIntranetData = new TestableItem( "CFLY.Intranet.Data" );
            cflyIntranetData.Requires.Add( cflyIntranet );
            cofely.Children.Add( cflyIntranetData );

            var cflyGed = new TestableItem( "CFLY.Ged.Extensions" );
            cflyGed.Requires.Add( cflyIntranetData );
            cofely.Children.Add( cflyGed );
        }

        var fmClient = new TestableContainer( "Feedermarket-Client.sln" );
        {
            var fmClientOp = new TestableItem( "Feedermarket.Client.Operation" );
            fmClientOp.Requires.Add( fmBuildPckg );
            fmClient.Children.Add( fmClientOp );
        }
        var fmClientOpPckg = new TestableItem( "NuGet:Feedermarket.Client.Operations" );
        fmClientOpPckg.Requires.Add( fmClient );

        var fmFunctions = new TestableContainer( "Feedermarket-Functions.sln" );
        {
            var database = new TestableItem( "Database" );
            fmFunctions.Children.Add( database );
            database.Requires.Add( fmClientOpPckg );
        }

        var options = new DependencySorterOptions()
        {
            HookInput = items => items.Trace( TestHelper.Monitor ),
            HookOutput = items => items.Trace( TestHelper.Monitor )
        };

        var r = DependencySorter.OrderItems( TestHelper.Monitor, options, cofely, fmBuildings, fmClient, fmFunctions );
        ResultChecker.SimpleCheckAndReset( r );
    }
}
