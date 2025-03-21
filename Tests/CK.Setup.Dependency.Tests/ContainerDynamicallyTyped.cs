#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\ContainerDynamicallyTyped.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using NUnit.Framework;
using CK.Core;
using static CK.Testing.MonitorTestHelper;
using Shouldly;

namespace CK.Setup.Dependency.Tests;

[TestFixture]
public class ContainerDynamicallyTyped
{
    [Test]
    public void EmptyContainer()
    {
        {
            var c = new TestableContainer( DependentItemKind.Item, "C" );
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, c );
                r.AssertOrdered( "C" );
                ResultChecker.SimpleCheckAndReset( r );
            }
        }
        {
            var c = new TestableContainer( DependentItemKind.Group, "C" );
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, c );
                r.AssertOrdered( "C.Head", "C" );
                ResultChecker.SimpleCheckAndReset( r );
            }
        }
    }

    [Test]
    public void AutoChildrenRegistration()
    {
        var c = new TestableContainer( DependentItemKind.Item, "C", new TestableItem( "A" ), new TestableItem( "B" ) );

        var r = DependencySorter.OrderItems( TestHelper.Monitor, c );
        Throw.Assert( r.HasStructureError );
        r.LogError( TestHelper.Monitor );
    }

    [Test]
    public void AutoContainerRegistration()
    {
        var c = new TestableContainer( DependentItemKind.Item, "ZeContainer" );
        var e = new TestableItem( "E" );
        e.Container = c;

        var r = DependencySorter.OrderItems( TestHelper.Monitor, e );
        Throw.Assert( r.HasStructureError );
        r.LogError( TestHelper.Monitor );
    }

    [Test]
    public void ThreeContainersByName()
    {
        {
            var c0 = new TestableContainer( DependentItemKind.Item, "C0" );
            var c1 = new TestableContainer( "C1", "⊏C0" );
            var c2 = new TestableContainer( "C2", "⊏C1" );
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c2, c0, c1 );
            r.LogError( TestHelper.Monitor );
            Throw.Assert( r.HasStructureError );
        }
        {
            var c0 = new TestableContainer( "C0" );
            var c1 = new TestableContainer( DependentItemKind.Item, "C1", "⊏C0" );
            var c2 = new TestableContainer( "C2", "⊏C1" );
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c2, c0, c1 );
            r.LogError( TestHelper.Monitor );
            Throw.Assert( r.HasStructureError );
        }
        {
            var c0 = new TestableContainer( "C0" );
            var c1 = new TestableContainer( "C1", "⊏C0" );
            var c2 = new TestableContainer( DependentItemKind.Item, "C2", "⊏C1" );
            var r = DependencySorter.OrderItems( TestHelper.Monitor, c2, c0, c1 );
            r.HasStructureError.ShouldBeFalse( "Success since c2 has no items." );
        }
    }

    [Test]
    public void PureGroups()
    {
        using( TestableItem.IgnoreCheckCount() )
        {
            var c0 = new TestableContainer( "C0" );
            var gA = new TestableContainer( DependentItemKind.Group, "GA", "∋C0" );
            var gB = new TestableContainer( DependentItemKind.Group, "GB", "∋C0" );
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, gA, c0, gB );
                Throw.Assert( r.IsComplete );
                r.AssertOrdered( "GA.Head", "GB.Head", "C0.Head", "C0", "GA", "GB" );
            }
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, true, gA, c0, gB );
                Throw.Assert( r.IsComplete );
                r.AssertOrdered( "GB.Head", "GA.Head", "C0.Head", "C0", "GB", "GA" );
            }
        }
        using( TestableItem.IgnoreCheckCount() )
        {
            var c0 = new TestableContainer( "C0" );
            var g1 = new TestableContainer( DependentItemKind.Group, "G1", new TestableItem( "Alpha" ) );
            var gA = new TestableContainer( DependentItemKind.Group, "GA", g1 );
            var gB = new TestableContainer( DependentItemKind.Group, "GB", "G1" );
            gA.Container = c0;
            gB.Container = c0;
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, gA, c0, g1, gB );
                Throw.Assert( r.IsComplete );
                r.AssertOrdered( "C0.Head", "GA.Head", "GB.Head", "G1.Head", "Alpha", "G1", "GA", "GB", "C0" );
            }
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, true, gA, c0, g1, gB );
                Throw.Assert( r.IsComplete );
                r.AssertOrdered( "C0.Head", "GB.Head", "GA.Head", "G1.Head", "Alpha", "G1", "GB", "GA", "C0" );
            }
            gA.Container = null;
            gB.Container = null;
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, gA, gB, g1, c0 );
                Throw.Assert( r.IsComplete );
                r.AssertOrdered( "C0.Head", "GA.Head", "GB.Head", "C0", "G1.Head", "Alpha", "G1", "GA", "GB" );
            }
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, true, gA, gB, g1, c0 );
                Throw.Assert( r.IsComplete );
                r.AssertOrdered( "GB.Head", "GA.Head", "C0.Head", "G1.Head", "C0", "Alpha", "G1", "GB", "GA" );
            }
        }
    }
}
