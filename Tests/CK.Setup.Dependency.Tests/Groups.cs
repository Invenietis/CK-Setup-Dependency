#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\Groups.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

namespace CK.Setup.Dependency.Tests;

[TestFixture]
public class Groups
{

    [Test]
    public void DiscoverByGroup()
    {
        using( TestableItem.IgnoreCheckCount() )
        {
            var g = new TestableContainer( DependentItemKind.Group, "G" );
            var c = new TestableContainer( DependentItemKind.Container, "C" );
            var i = new TestableContainer( DependentItemKind.Item, "I" );
            g.Add( i );
            i.Container = c;
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, i, c, g );
                r.AssertOrdered( "C.Head", "G.Head", "I", "C", "G" );
            }
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, c, i, g );
                r.AssertOrdered( "C.Head", "G.Head", "I", "C", "G" );
            }
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, g );
                r.AssertOrdered( "C.Head", "G.Head", "I", "C", "G" );
            }
        }
    }

    [Test]
    public void InsideAnotherOne()
    {
        using( TestableItem.IgnoreCheckCount() )
        {
            var g1 = new TestableContainer( DependentItemKind.Group, "G1" );
            var g2 = new TestableContainer( DependentItemKind.Group, "G2" );
            var g3 = new TestableContainer( DependentItemKind.Group, "G3" );
            g3.Groups.Add( g2 );
            g2.Groups.Add( g1 );
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, g1, g2, g3 );
                CheckG1G2G3( r );

            }
            {
                // Auto discovering by Groups.
                var r = DependencySorter.OrderItems( TestHelper.Monitor, g3 );
                CheckG1G2G3( r );
            }
            g2.Children.Add( g3 );
            {
                // Auto discovering: G1 by Groups and G3 by Children.
                var r = DependencySorter.OrderItems( TestHelper.Monitor, g2 );
                CheckG1G2G3( r );
            }
            g1.Children.Add( g2 );
            {
                // Auto discovering by Children.
                var r = DependencySorter.OrderItems( TestHelper.Monitor, g1 );
                CheckG1G2G3( r );
            }
            g3.Groups.Remove( g2 );
            g2.Groups.Remove( g1 );
            {
                // Auto discovering by Children (no redundant Groups relations).
                var r = DependencySorter.OrderItems( TestHelper.Monitor, g1 );
                CheckG1G2G3( r );
            }
        }
    }

    [Test]
    public void InsideAnotherOneByName()
    {
        using( TestableItem.IgnoreCheckCount() )
        {
            var g1 = new TestableContainer( DependentItemKind.Group, "G1" );
            var g2 = new TestableContainer( DependentItemKind.Group, "G2" );
            var g3 = new TestableContainer( DependentItemKind.Group, "G3" );
            g3.Add( "∈G2" );
            g2.Add( "∈G1" );
            {
                var r = DependencySorter<IDependentItem>.OrderItems( TestHelper.Monitor, g1, g2, g3 );
                CheckG1G2G3( r );

            }
            g3.Groups.Add( g2 );
            g2.Groups.Add( g1 );
            {
                // Auto discovering by Groups (and no clashes with names).
                var r = DependencySorter<IDependentItem>.OrderItems( TestHelper.Monitor, g3 );
                CheckG1G2G3( r );
            }
        }
    }

    private static void CheckG1G2G3( IDependencySorterResult r )
    {
        r.IsComplete.ShouldBeTrue();
        r.AssertOrdered( "G1.Head", "G2.Head", "G3.Head", "G3", "G2", "G1" );

        var s3 = r.SortedItems[3]; s3.FullName.ShouldBe( "G3" );
        var s2 = r.SortedItems[4]; s2.FullName.ShouldBe( "G2" );
        var s1 = r.SortedItems[5]; s1.FullName.ShouldBe( "G1" );
        s1.Children.ShouldHaveSingleItem().ShouldBeSameAs( s2 );
        s2.Children.ShouldHaveSingleItem().ShouldBeSameAs(s3);
        s3.Children.ShouldBeEmpty();

        s1.Groups.ShouldBeEmpty();
        s2.Groups.ShouldHaveSingleItem().ShouldBeSameAs(s1);
        s3.Groups.ShouldHaveSingleItem().ShouldBeSameAs(s2);
    }

}
