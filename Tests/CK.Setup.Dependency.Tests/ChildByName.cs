#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\ChildByName.cs) is part of CK-Database. 
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
    public class ChildByName
    {
        [Test]
        public void JustContainers()
        {
            var cB = new TestableContainer( "CB", "⊐ CA" );
            var cA = new TestableContainer( "CA" );
            {
                // Starting by CA.
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, cA, cB );
                Assert.That( r.IsComplete );
                r.AssertOrdered( "CB.Head", "CA.Head", "CA", "CB" );
                ResultChecker.SimpleCheck( r );
            }
            {
                // Starting by CB.
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, cB, cA );
                Assert.That( r.IsComplete );
                r.AssertOrdered( "CB.Head", "CA.Head", "CA", "CB" );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void SomeItems()
        {
            var cB = new TestableContainer( "CB", "⊐ OB" );
            var oB = new TestableItem( "OB" );
            {
                // Starting with the Container.
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, cB, oB );
                Assert.That( r.IsComplete );
                r.AssertOrdered( "CB.Head", "OB", "CB" );
                ResultChecker.SimpleCheck( r );
            }
            {
                // Starting with the Item.
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, oB, cB );
                Assert.That( r.IsComplete );
                r.AssertOrdered( "CB.Head", "OB", "CB" );
                ResultChecker.SimpleCheck( r );
            }
            var cA = new TestableContainer( "CA", "⊐ OA" );
            cB.Add( "⊐ CA" );
            var oA = new TestableItem( "OA" );
            {
                // Starting with the Containers.
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, cB, oB, cA, oA );
                Assert.That( r.IsComplete );
                r.AssertOrdered( "CB.Head", "CA.Head", "OB", "OA", "CA", "CB" );
                ResultChecker.SimpleCheck( r );
            }
            {
                // Starting with the Items.
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, oB, oA, cB, cA );
                Assert.That( r.IsComplete );
                r.AssertOrdered( "CB.Head", "CA.Head", "OB", "OA", "CA", "CB" );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void MissingChild()
        {
            var cB = new TestableContainer( "CB", "⊐ CA" );
            {
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, cB );
                Assert.That( r.IsComplete, Is.False );
                r.AssertOrdered( "CB.Head", "CB" );
                Assert.That( r.HasStructureError, Is.True );
                Assert.That( r.ItemIssues.Count, Is.EqualTo( 1 ) );
                Assert.That( r.ItemIssues[0].StructureError, Is.EqualTo( DependentItemStructureError.MissingNamedChild ) );
                Assert.That( r.ItemIssues[0].MissingChildren.Single(), Is.EqualTo( "CA" ) );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void ExtraneousContainer()
        {
            var childOfCB2 = new TestableItem( "ChildOfCB2" );
            var cB1 = new TestableContainer( "CB1", "⊐ ChildOfCB2" );
            var cB2 = new TestableContainer( "CB2", childOfCB2 );
            {
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, cB1, cB2 );
                Assert.That( r.IsComplete, Is.False );
                r.AssertOrdered( "CB1.Head", "CB2.Head", "CB1", "ChildOfCB2", "CB2" );
                Assert.That( r.HasStructureError, Is.True );
                Assert.That( r.ItemIssues.Count, Is.EqualTo( 1 ) );

                var issue = r.ItemIssues.Single( i => i.Item == childOfCB2 );
                Assert.That( issue.StructureError, Is.EqualTo( DependentItemStructureError.MultipleContainer ) );
                Assert.That( issue.ExtraneousContainers.Single(), Is.EqualTo( "CB1" ) );
                
                ResultChecker.SimpleCheck( r );
            }
        }


        [Test]
        public void MultipleStructureErrors()
        {
            var childOfCB2 = new TestableItem( "ChildOfCB2" );
            var cB1 = new TestableContainer( "CB1", "⊐ MissingChild", "⊐ ChildOfCB2" );
            var cB2 = new TestableContainer( "CB2", "⊐ MissingChild", "⊏ MissingContainer", "⇀ MissingDependency", childOfCB2 );
            var cB3 = new TestableContainer( "CB3", "⊏ ChildOfCB2", "⇀ MissingDependency" );
            // This "discovers" an homonym.
            cB3.RelatedItems.Add( new TestableItem( "CB1" ) );
            {
                var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, cB1, cB2, cB3 );
                Assert.That( r.IsComplete, Is.False );
                r.AssertOrdered( "CB1.Head", "CB2.Head", "CB3.Head", "CB1", "CB3", "ChildOfCB2", "CB2" );
                Assert.That( r.HasStructureError, Is.True );
                Assert.That( r.ItemIssues.Count, Is.EqualTo( 4 ) );
                
                var issue1 = r.ItemIssues.Single( i => i.Item == cB1 );
                Assert.That( issue1.StructureError, Is.EqualTo( DependentItemStructureError.MissingNamedChild | DependentItemStructureError.Homonym ) );
                Assert.That( issue1.MissingChildren.Single(), Is.EqualTo( "MissingChild" ) );
                Assert.That( issue1.Homonyms.Single().FullName, Is.EqualTo( "CB1" ) );
                
                var issue2 = r.ItemIssues.Single( i => i.Item == cB2 );
                Assert.That( issue2.StructureError, Is.EqualTo( DependentItemStructureError.MissingNamedChild | DependentItemStructureError.MissingNamedContainer | DependentItemStructureError.MissingDependency ) );
                Assert.That( issue2.MissingChildren.Single(), Is.EqualTo( "MissingChild" ) );
                Assert.That( issue2.MissingDependencies.Single(), Is.EqualTo( "MissingDependency" ) );

                var issue3 = r.ItemIssues.Single( i => i.Item == cB3 );
                Assert.That( issue3.StructureError, Is.EqualTo( DependentItemStructureError.ExistingItemIsNotAContainer | DependentItemStructureError.MissingDependency ) );
                Assert.That( issue3.MissingDependencies.Single(), Is.EqualTo( "MissingDependency" ) );

                var issue4 = r.ItemIssues.Single( i => i.Item == childOfCB2 );
                Assert.That( issue4.StructureError, Is.EqualTo( DependentItemStructureError.MultipleContainer ) );
                Assert.That( issue4.ExtraneousContainers.Single(), Is.EqualTo( "CB1" ) );

                ResultChecker.SimpleCheck( r );
            }
        }

    }
}
