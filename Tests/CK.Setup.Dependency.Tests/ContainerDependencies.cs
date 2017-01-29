#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\ContainerDependencies.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;

namespace CK.Setup.Dependency.Tests
{
    [TestFixture]
    public class ContainerDependencies
    {
        [Test]
        public void an_empty_container_has_its_head_before_its_content()
        {
            var c = new TestableContainer( "C" );
            {
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, c );
                r.AssertOrdered( "C.Head", "C" );
                ResultChecker.SimpleCheck( r );
            }
            {
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, true, c );
                r.AssertOrdered( "C.Head", "C" );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void container_children_are_automatically_registered()
        {
            var c = new TestableContainer( "C", new TestableItem( "A" ), new TestableItem( "B" ) );

            var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, c );
            Assert.That( r.SortedItems.Count, Is.EqualTo( 4 ) );

            Assert.That( r.SortedItems[0].IsGroupHead, "Head of Container." );
            Assert.That( r.SortedItems[1].Item.FullName, Is.EqualTo( "A" ), "Lexical order." );
            Assert.That( r.SortedItems[2].Item.FullName, Is.EqualTo( "B" ), "Lexical order." );
            Assert.That( r.SortedItems[3].Item.FullName, Is.EqualTo( "C" ), "Container" );
            new ResultChecker( r ).CheckRecurse( c.FullName );
            ResultChecker.SimpleCheck( r );
            r.CheckChildren( "C", "A,B" );
        }

        [Test]
        public void a_container_is_automatically_registered_by_any_of_its_children()
        {
            var c = new TestableContainer( "ZeContainer", new TestableItem( "A" ), new TestableItem( "B" ) );
            var e = new TestableItem( "E" );
            c.Add( e );

            var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, e );
            Assert.That( r.SortedItems.Count, Is.EqualTo( 5 ) );

            Assert.That( r.SortedItems[0].IsGroupHead, "Head of Container." );
            Assert.That( r.SortedItems[1].Item.FullName, Is.EqualTo( "A" ), "Lexical order." );
            Assert.That( r.SortedItems[2].Item.FullName, Is.EqualTo( "B" ), "Lexical order." );
            Assert.That( r.SortedItems[3].Item.FullName, Is.EqualTo( "E" ), "Lexical order." );
            Assert.That( r.SortedItems[4].Item.FullName, Is.EqualTo( "ZeContainer" ), "Container" );

            new ResultChecker( r ).CheckRecurse( c.FullName, e.FullName );
            ResultChecker.SimpleCheck( r );
            r.CheckChildren( "ZeContainer", "A,B,E" );
        }

        [Test]
        public void one_package_with_its_model()
        {
            var pA = new TestableContainer( "A" );
            var pAModel = new TestableContainer( "Model.A" );

            Action test = () =>
                { 
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, pAModel, pA );
                    ResultChecker.SimpleCheck( r );
                    var rRevert = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, true, pAModel, pA );
                    ResultChecker.SimpleCheck( rRevert );
            
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
                    var r1 = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, pAModel, pA, pBModel, pB );
                    ResultChecker.SimpleCheck( r1 );
                    var r2 = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, true, pAModel, pA, pBModel, pB );
                    ResultChecker.SimpleCheck( r2 );
            
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

        [Test]
        public void packages_with_model_and_objects()
        {
            var pA = new TestableContainer( "A" );
            var pAModel = new TestableContainer( "Model.A" );
            var pAObjects = new TestableContainer( "Objects.A" );
            var pB = new TestableContainer( "B" );
            var pBModel = new TestableContainer( "Model.B" );
            var pBObjects = new TestableContainer( "Objects.B" );
            var all = new[]{ pA, pAModel, pAObjects, pB, pBModel, pBObjects };

            Action testAndRestore = () =>
            {
                var r1 = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, all );
                ResultChecker.SimpleCheck( r1 );
                var r2 = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, true, all );
                ResultChecker.SimpleCheck( r2 );

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
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, c2, c0, c1 );
                r.AssertOrdered( "C0.Head", "A", "B", "C", "C0", "C1.Head", "X", "C1", "C2.Head", "Y", "C2" );
                new ResultChecker( r ).CheckRecurse( "C0", "C1", "C2" );
                ResultChecker.SimpleCheck( r );
                r.CheckChildren( "C0", "A,B,C" );
            }
            {
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, c0, c1, c2 );
                r.AssertOrdered( "C0.Head", "A", "B", "C", "C0", "C1.Head", "X", "C1", "C2.Head", "Y", "C2" );
                new ResultChecker( r ).CheckRecurse( "C0", "C1", "C2" );
                ResultChecker.SimpleCheck( r );
                r.CheckChildren( "C0", "A,B,C" );
            }
            {
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, c2, c1, c0 );
                r.AssertOrdered( "C0.Head", "A", "B", "C", "C0", "C1.Head", "X", "C1", "C2.Head", "Y", "C2" );
                new ResultChecker( r ).CheckRecurse( "C0", "C1", "C2" );
                ResultChecker.SimpleCheck( r );
                r.CheckChildren( "C0", "A,B,C" );
            }
        }

        [Test]
        public void ItemToContainer()
        {
            var c0 = new TestableContainer( "C0", new TestableItem( "A" ), new TestableItem( "B" ), new TestableItem( "C" ) );
            var c1 = new TestableContainer( "C1", new TestableItem( "X", "⇀C0" ) );
            var c2 = new TestableContainer( "C2", new TestableItem( "Y", "⇀C1" ) );
            var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, c2, c0, c1 );
            new ResultChecker( r ).CheckRecurse( "C0", "C1", "C2" );
            ResultChecker.SimpleCheck( r );
            r.CheckChildren( "C0", "A,B,C" );
            r.CheckChildren( "C1", "X" );
            r.CheckChildren( "C2", "Y" );
        }


        [Test]
        public void MissingDependencies()
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
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, c );
                Assert.That( r.ItemIssues[0].Item.FullName, Is.EqualTo( "Root" ) );
                Assert.That( r.ItemIssues[0].RequiredMissingCount, Is.EqualTo( 1 ) );
                Assert.That( r.ItemIssues[0].MissingDependencies.Count(), Is.EqualTo( 1 ) );
                Assert.That( r.ItemIssues[0].MissingDependencies.First(), Is.EqualTo( "Direct" ) );

                Assert.That( r.ItemIssues[1].Item.FullName, Is.EqualTo( "Nuage" ) );
                Assert.That( r.ItemIssues[1].RequiredMissingCount, Is.EqualTo( 0 ) );
                Assert.That( r.ItemIssues[1].MissingDependencies.Count(), Is.EqualTo( 1 ) );
                Assert.That( r.ItemIssues[1].MissingDependencies.First(), Is.EqualTo( "?OptDirect" ) );

                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void CycleDetection0()
        {
            var c = new TestableContainer( "A", "⇀ A" );
            var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, c );
            Assert.That( r.CycleDetected, Is.Not.Null );
            Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ A ⇀ A" ) );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void CycleDetection1()
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
            var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, c );
            Assert.That( r.CycleDetected, Is.Not.Null );
            Assert.That( r.SortedItems, Is.Null );
            // The detected cycle depends on the algorithm. 
            // This works here because since we register the Root, the last registered child is Nuage: we know
            // that the cycle starts (and ends) with Nuage because children are in linked list (added at the head).
            // (This remarks is valid for the other CycleDetection below.)
            Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ Nuage ⇀ Pierre ⇀ Stratus ⊏ Nuage" ) );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void CycleDetection2()
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
            var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, c );
            Assert.That( r.CycleDetected, Is.Not.Null );
            Assert.That( r.SortedItems, Is.Null );
            // See remark in CycleDetection1.
            Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ Nuage ⇀ Pierre ⊐ Rubis ⇀ Stratus ⊏ Nuage" ) );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void CycleDetection3()
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
            var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, c );
            Assert.That( r.CycleDetected, Is.Not.Null );
            Assert.That( r.SortedItems, Is.Null );
            // See remark in CycleDetection1.
            // Here we can see the RequiredByRequires relation: ⇌
            Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ Nuage ⇀ Pierre ⊐ Rubis ⇌ Stratus ⊏ Nuage" ) );
            ResultChecker.SimpleCheck( r );
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
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, c );
                Assert.That( r.ItemIssues.Any( m => m.MissingDependencies.Contains( "AMissingDependency" ) ) );
                new ResultChecker( r ).CheckRecurse( "Root" );
                ResultChecker.SimpleCheck( r );
            }
            {
                // Ordering handles duplicates.
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, new IDependentItem[]{ c, c } );
                Assert.That( r.ItemIssues.Any( m => m.MissingDependencies.Contains( "AMissingDependency" ) ) );
                new ResultChecker( r ).CheckRecurse( "Root" );
                ResultChecker.SimpleCheck( r );
            }
            {
                // Ordering handles duplicates.
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, new IDependentItem[] { c, c }.Concat( c.Children.Cast<IDependentItem>() ).Concat( c.Children.Cast<IDependentItem>() ), null );
                Assert.That( r.ItemIssues.Any( m => m.MissingDependencies.Contains( "AMissingDependency" ) ) );
                new ResultChecker( r ).CheckRecurse( "Root" );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void SimpleGraph()
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

            var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, pAB, oA, oB, pABLevel1, oBLevel1 );
            Assert.That( r.IsComplete );
            r.AssertOrdered( "PackageForAB.Head", "A", "B", "PackageForAB", "PackageForABLevel1.Head", "ObjectBLevel1", "PackageForABLevel1" );
            r.CheckChildren( "PackageForAB", "A,B" );
            r.CheckChildren( "PackageForABLevel1", "ObjectBLevel1" );
        }
    }
}
