#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\Groups.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace CK.Setup.Dependency.Tests
{
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
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, i, c, g );
                    r.AssertOrdered( "C.Head", "G.Head", "I", "C", "G" );
                }
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, c, i, g );
                    r.AssertOrdered( "C.Head", "G.Head", "I", "C", "G" );
                }
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, g );
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
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, g1, g2, g3 );
                    CheckG1G2G3( r );

                }
                {
                    // Auto discovering by Groups.
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, g3 );
                    CheckG1G2G3( r );
                }
                g2.Children.Add( g3 );
                {
                    // Auto discovering: G1 by Groups and G3 by Children.
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, g2 );
                    CheckG1G2G3( r );
                }
                g1.Children.Add( g2 );
                {
                    // Auto discovering by Children.
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, g1 );
                    CheckG1G2G3( r );
                }
                g3.Groups.Remove( g2 );
                g2.Groups.Remove( g1 );
                {
                    // Auto discovering by Children (no redundant Groups relations).
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, g1 );
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
                    var r = DependencySorter<IDependentItem>.OrderItems( TestHelper.ConsoleMonitor, g1, g2, g3 );
                    CheckG1G2G3( r );

                }
                g3.Groups.Add( g2 );
                g2.Groups.Add( g1 );
                {
                    // Auto discovering by Groups (and no clashes with names).
                    var r = DependencySorter<IDependentItem>.OrderItems( TestHelper.ConsoleMonitor, g3 );
                    CheckG1G2G3( r );
                }
            }
        }

        private static void CheckG1G2G3( IDependencySorterResult r )
        {
            Assert.That( r.IsComplete );
            r.AssertOrdered( "G1.Head", "G2.Head", "G3.Head", "G3", "G2", "G1" );

            var s3 = r.SortedItems[3]; Assert.That( s3.FullName == "G3" );
            var s2 = r.SortedItems[4]; Assert.That( s2.FullName == "G2" );
            var s1 = r.SortedItems[5]; Assert.That( s1.FullName == "G1" );
            Assert.That( s1.Children.Single() == s2 );
            Assert.That( s2.Children.Single() == s3 );
            Assert.That( s3.Children.Any() == false );

            Assert.That( s1.Groups.Any() == false );
            Assert.That( s2.Groups.Single() == s1 );
            Assert.That( s3.Groups.Single() == s2 );
        }

    }
}
