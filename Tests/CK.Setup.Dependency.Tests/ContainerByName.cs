#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\ContainerByName.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Linq;
using NUnit.Framework;
using CK.Core;
using static CK.Testing.MonitorTestHelper;
using Shouldly;

namespace CK.Setup.Dependency.Tests;

[TestFixture]
public class ContainerByName
{
    [Test]
    public void JustContainers()
    {
        var cB = new TestableContainer( "CB" );
        var cA = new TestableContainer( "CA", "⊏ CB" );
        {
            // Starting by CA.
            var r = DependencySorter.OrderItems( TestHelper.Monitor, cA, cB );
            Throw.Assert( r.IsComplete );
            r.AssertOrdered( "CB.Head", "CA.Head", "CA", "CB" );
            ResultChecker.SimpleCheckAndReset( r );
            r.CheckChildren( "CB", "CA" );
        }
        {
            // Starting by CB.
            var r = DependencySorter.OrderItems( TestHelper.Monitor, cB, cA );
            Throw.Assert( r.IsComplete );
            r.AssertOrdered( "CB.Head", "CA.Head", "CA", "CB" );
            ResultChecker.SimpleCheckAndReset( r );
            r.CheckChildren( "CB", "CA" );
        }
    }

    [Test]
    public void SomeItems()
    {
        var c = new TestableContainer( "C" );
        var o1 = new TestableItem( "O1", "⊏ C" );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c, o1 );
            r.AssertOrdered( "C.Head", "O1", "C" );
            ResultChecker.SimpleCheckAndReset( r );
            r.CheckChildren( "C", "O1" );
        }
        var o2 = new TestableItem( "O2", "⊏ O1" );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c, o1, o2 );
            r.IsComplete.ShouldBeFalse();
            r.HasStructureError.ShouldBeTrue();;
            ResultChecker.SimpleCheckAndReset( r );
        }
        o2.Add( "⊏ C", "↽ O1" );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c, o1, o2 );
            r.AssertOrdered( "C.Head", "O2", "O1", "C" );
            ResultChecker.SimpleCheckAndReset( r );
            r.CheckChildren( "C", "O1,O2" );
        }
        var sub = new TestableItem( "Cycle", "⊏ C", "⇀ C" );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c, o1, o2, sub );
            r.CycleExplainedString.ShouldBe("↳ C ⊐ Cycle ⇀ C");
            ResultChecker.SimpleCheckAndReset( r );
        }
    }

    [Test]
    public void MissingContainer()
    {
        var o = new TestableItem( "O1", "⊏ C" );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, o );
            r.IsComplete.ShouldBeFalse();
            r.AssertOrdered( "O1" );
            r.HasStructureError.ShouldBeTrue();;
            r.ItemIssues.Count.ShouldBe(1);
            r.ItemIssues[0].StructureError.ShouldBe(DependentItemStructureError.MissingNamedContainer);
            r.ItemIssues[0].MissingChildren.ShouldBeEmpty();
            ResultChecker.SimpleCheckAndReset( r );
        }
    }

    [Test]
    public void MonoCycle()
    {
        var c = new TestableContainer( "C", "⇀ C" );
        var o1 = new TestableItem( "O1", "⊏ C" );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c, o1 );
            r.CycleExplainedString.ShouldBe("↳ C ⇀ C");
            ResultChecker.SimpleCheckAndReset( r );
        }
    }

    [Test]
    public void AutoContains()
    {
        var c = new TestableContainer( "C", "O1", "⊐ C" );
        var o1 = new TestableItem( "O1" );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c, o1 );
            r.CycleExplainedString.ShouldBe("↳ C ⊏ C");
            ResultChecker.SimpleCheckAndReset( r );
        }
    }


    [Test]
    public void RecurseAutoContains()
    {
        var c = new TestableContainer( "C", "⊏ D" );
        var o1 = new TestableItem( "O1", "⊏ C" );
        var d = new TestableContainer( "D", "⊏ C" );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c, o1, d );
            r.CycleExplainedString.ShouldBe("↳ C ⊏ D ⊏ C");
            ResultChecker.SimpleCheckAndReset( r );
        }
    }

    [Test]
    public void MultiContainerByName()
    {
        var o1 = new TestableItem( "O1" );
        var c = new TestableContainer( "C", o1 );

        Throw.Assert( c.Children.Contains( o1 ) && o1.Container == c );
        o1.Add( "⊏ D" );
        Throw.Assert( c.Children.Contains( o1 ) && o1.Container != c );

        var d = new TestableContainer( "D" );

        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c, o1, d );
            r.IsComplete.ShouldBeFalse();
            r.HasStructureError.ShouldBeTrue();;
            r.ItemIssues[0].StructureError.ShouldBe(DependentItemStructureError.MultipleContainer);
            r.ItemIssues[0].Item.FullName.ShouldBe("O1");
            r.ItemIssues[0].Item.Container.FullName.ShouldBe("D");
            r.ItemIssues[0].ExtraneousContainers.Single().ShouldBe("C");
            ResultChecker.SimpleCheckAndReset( r );
        }

        {
            // Starting by o1: its container is still C (the extraneous container is still D)
            // since named containers binding is deferred: c.Children wins one again.
            // Whatever the order is, what is important is that IsComplete is false and a ExtraneousContainers is detected.
            var r = DependencySorter.OrderItems( TestHelper.Monitor, o1, c, d );
            r.IsComplete.ShouldBeFalse();
            r.HasStructureError.ShouldBeTrue();;
            r.ItemIssues[0].StructureError.ShouldBe(DependentItemStructureError.MultipleContainer);
            r.ItemIssues[0].Item.FullName.ShouldBe("O1");
            r.ItemIssues[0].Item.Container.FullName.ShouldBe("D");
            r.ItemIssues[0].ExtraneousContainers.Single().ShouldBe("C");
            ResultChecker.SimpleCheckAndReset( r );
        }
    }

    [Test]
    public void MultiContainerByref()
    {
        var o1 = new TestableItem( "O1" );
        var c = new TestableContainer( "C", o1 );
        var d = new TestableContainer( "D" );

        Throw.Assert( c.Children.Contains( o1 ) && o1.Container == c );
        o1.Container = d;
        Throw.Assert( c.Children.Contains( o1 ) && o1.Container != c );

        {
            // Starting by C: O1 is discovered by C.Children: the extraneous container is D.
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c, o1, d );
            r.IsComplete.ShouldBeFalse();
            r.HasStructureError.ShouldBeTrue();
            r.ItemIssues[0].StructureError.ShouldBe(DependentItemStructureError.MultipleContainer);
            r.ItemIssues[0].Item.FullName.ShouldBe("O1");
            r.ItemIssues[0].Item.Container.FullName.ShouldBe("D");
            r.ItemIssues[0].ExtraneousContainers.ShouldHaveSingleItem().ShouldBe("C");
            ResultChecker.SimpleCheckAndReset( r );
        }

        {
            // Starting by o1: its container is D, the extraneous container is C.
            var r = DependencySorter.OrderItems( TestHelper.Monitor, o1, c, d );
            r.IsComplete.ShouldBeFalse();
            r.HasStructureError.ShouldBeTrue();;
            r.ItemIssues[0].StructureError.ShouldBe(DependentItemStructureError.MultipleContainer);
            r.ItemIssues[0].Item.FullName.ShouldBe("O1");
            r.ItemIssues[0].Item.Container.FullName.ShouldBe("D");
            r.ItemIssues[0].ExtraneousContainers.ShouldHaveSingleItem().ShouldBe("C");
            ResultChecker.SimpleCheckAndReset( r );
        }
    }

    [Test]
    public void WhenTheItemContainerIsNull()
    {
        var o1 = new TestableItem( "O1" );
        var c = new TestableContainer( "C", o1 );

        Throw.Assert( c.Children.Contains( o1 ) && o1.Container == c );
        o1.Container = null;
        Throw.Assert( c.Children.Contains( o1 ) && o1.Container == null );

        {
            // Starting by C: O1 is discovered by C.Children: the container becomes C since O1 does not say anything.
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c, o1 );
            r.IsComplete.ShouldBeTrue();
            r.HasStructureError.ShouldBeFalse();;
            r.AssertOrdered( "C.Head", "O1", "C" );
            r.SortedItems[1].Container.FullName.ShouldBe("C");
            ResultChecker.SimpleCheckAndReset( r );
        }

        {
            // Starting by O1: its container becomes C.
            var r = DependencySorter.OrderItems( TestHelper.Monitor, o1, c );
            r.IsComplete.ShouldBeTrue();
            r.HasStructureError.ShouldBeFalse();;
            r.AssertOrdered( "C.Head", "O1", "C" );
            r.SortedItems[1].Container.FullName.ShouldBe("C");
            ResultChecker.SimpleCheckAndReset( r );
        }


    }


}
