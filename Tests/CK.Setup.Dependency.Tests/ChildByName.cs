#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\ChildByName.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Linq;
using NUnit.Framework;
using CK.Core;
using static CK.Testing.MonitorTestHelper;
using Shouldly;


namespace CK.Setup.Dependency.Tests;

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
            var r = DependencySorter.OrderItems( TestHelper.Monitor, cA, cB );
            Throw.Assert( r.IsComplete );
            r.AssertOrdered( "CB.Head", "CA.Head", "CA", "CB" );
            ResultChecker.SimpleCheckAndReset( r );
        }
        {
            // Starting by CB.
            var r = DependencySorter.OrderItems( TestHelper.Monitor, cB, cA );
            Throw.Assert( r.IsComplete );
            r.AssertOrdered( "CB.Head", "CA.Head", "CA", "CB" );
            ResultChecker.SimpleCheckAndReset( r );
        }
    }

    [Test]
    public void SomeItems()
    {
        var cB = new TestableContainer( "CB", "⊐ OB" );
        var oB = new TestableItem( "OB" );
        {
            // Starting with the Container.
            var r = DependencySorter.OrderItems( TestHelper.Monitor, cB, oB );
            Throw.Assert( r.IsComplete );
            r.AssertOrdered( "CB.Head", "OB", "CB" );
            ResultChecker.SimpleCheckAndReset( r );
        }
        {
            // Starting with the Item.
            var r = DependencySorter.OrderItems( TestHelper.Monitor, oB, cB );
            Throw.Assert( r.IsComplete );
            r.AssertOrdered( "CB.Head", "OB", "CB" );
            ResultChecker.SimpleCheckAndReset( r );
        }
        var cA = new TestableContainer( "CA", "⊐ OA" );
        cB.Add( "⊐ CA" );
        var oA = new TestableItem( "OA" );
        {
            // Starting with the Containers.
            var r = DependencySorter.OrderItems( TestHelper.Monitor, cB, oB, cA, oA );
            Throw.Assert( r.IsComplete );
            r.AssertOrdered( "CB.Head", "CA.Head", "OB", "OA", "CA", "CB" );
            ResultChecker.SimpleCheckAndReset( r );
        }
        {
            // Starting with the Items.
            var r = DependencySorter.OrderItems( TestHelper.Monitor, oB, oA, cB, cA );
            Throw.Assert( r.IsComplete );
            r.AssertOrdered( "CB.Head", "CA.Head", "OB", "OA", "CA", "CB" );
            ResultChecker.SimpleCheckAndReset( r );
        }
    }

    [Test]
    public void MissingChild()
    {
        var cB = new TestableContainer( "CB", "⊐ CA" );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, cB );
            r.IsComplete.ShouldBeFalse();
            r.AssertOrdered( "CB.Head", "CB" );
            r.HasStructureError.ShouldBeTrue();
            r.ItemIssues.Count.ShouldBe( 1 );
            r.ItemIssues[0].StructureError.ShouldBe( DependentItemStructureError.MissingNamedChild );
            r.ItemIssues[0].MissingChildren.ShouldHaveSingleItem().ShouldBe("CA" );
            ResultChecker.SimpleCheckAndReset( r );
        }
    }

    [Test]
    public void ExtraneousContainer()
    {
        var childOfCB2 = new TestableItem( "ChildOfCB2" );
        var cB1 = new TestableContainer( "CB1", "⊐ ChildOfCB2" );
        var cB2 = new TestableContainer( "CB2", childOfCB2 );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, cB1, cB2 );
            Throw.Assert( !r.IsComplete );
            r.AssertOrdered( "CB1.Head", "CB2.Head", "CB1", "ChildOfCB2", "CB2" );
            r.HasStructureError.ShouldBeTrue();
            r.ItemIssues.Count.ShouldBe( 1 );

            var issue = r.ItemIssues.Single( i => i.Item == childOfCB2 );
            issue.StructureError.ShouldBe( DependentItemStructureError.MultipleContainer );
            issue.ExtraneousContainers.ShouldHaveSingleItem().ShouldBe( "CB1" );

            ResultChecker.SimpleCheckAndReset( r );
        }
    }

    [Test]
    public void MultipleStructureErrors_with_homonym_prevents_the_graph_ordering()
    {
        var childOfCB2 = new TestableItem( "ChildOfCB2" );
        var cB1 = new TestableContainer( "CB1", "⊐ MissingChild", "⊐ ChildOfCB2" );
        var cB2 = new TestableContainer( "CB2", "⊐ MissingChild", "⊏ MissingContainer", "⇀ MissingDependency", childOfCB2 );
        var cB3 = new TestableContainer( "CB3", "⊏ ChildOfCB2", "⇀ MissingDependency" );
        // This "discovers" an homonym.
        cB3.RelatedItems.Add( new TestableItem( "CB1" ) );
        var r = DependencySorter.OrderItems( TestHelper.Monitor, cB1, cB2, cB3 );
        Throw.Assert( !r.IsComplete );
        r.HasSevereStructureError.ShouldBeTrue();
        r.SortedItems.ShouldBeNull();
        r.ItemIssues.Count.ShouldBe( 4 );
        r.ItemIssues.SelectMany( i => i.Homonyms ).Count().ShouldBe( 1 );
    }

    [Test]
    public void MultipleStructureErrors()
    {
        var childOfCB2 = new TestableItem( "ChildOfCB2" );
        var cB1 = new TestableContainer( "CB1", "⊐ MissingChild", "⊐ ChildOfCB2" );
        var cB2 = new TestableContainer( "CB2", "⊐ MissingChild", "⊏ MissingContainer", "⇀ MissingDependency", childOfCB2 );
        var cB3 = new TestableContainer( "CB3", "⊏ ChildOfCB2", "⇀ MissingDependency" );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, cB1, cB2, cB3 );
            Throw.Assert( !r.IsComplete );
            r.AssertOrdered( "CB1.Head", "CB2.Head", "CB3.Head", "CB1", "CB3", "ChildOfCB2", "CB2" );
            r.HasStructureError.ShouldBeTrue();
            r.ItemIssues.Count.ShouldBe(4);

            var issue1 = r.ItemIssues.Single( i => i.Item == cB1 );
            issue1.StructureError.ShouldBe(DependentItemStructureError.MissingNamedChild);
            issue1.MissingChildren.ShouldHaveSingleItem().ShouldBe("MissingChild");

            var issue2 = r.ItemIssues.Single( i => i.Item == cB2 );
            issue2.StructureError.ShouldBe(DependentItemStructureError.MissingNamedChild | DependentItemStructureError.MissingNamedContainer | DependentItemStructureError.MissingDependency);
            issue2.MissingChildren.ShouldHaveSingleItem().ShouldBe("MissingChild");
            issue2.MissingDependencies.ShouldHaveSingleItem().ShouldBe("MissingDependency");

            var issue3 = r.ItemIssues.Single( i => i.Item == cB3 );
            issue3.StructureError.ShouldBe(DependentItemStructureError.ExistingItemIsNotAContainer | DependentItemStructureError.MissingDependency);
            issue3.MissingDependencies.ShouldHaveSingleItem().ShouldBe("MissingDependency");

            var issue4 = r.ItemIssues.Single( i => i.Item == childOfCB2 );
            issue4.StructureError.ShouldBe(DependentItemStructureError.MultipleContainer);
            issue4.ExtraneousContainers.ShouldHaveSingleItem().ShouldBe("CB1");

            ResultChecker.SimpleCheckAndReset( r );
        }
    }

}
