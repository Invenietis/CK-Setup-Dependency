#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\ByNames.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using static CK.Testing.MonitorTestHelper;

namespace CK.Setup.Dependency.Tests;

[TestFixture]
public class ByNames
{
    [Test]
    public void NamesAreCaseSensitive()
    {
        var cA = new TestableContainer( "A", "⊐ b" );
        var cB = new TestableContainer( "B" );
        {
            // Starting by CA.
            var r = DependencySorter.OrderItems( TestHelper.Monitor, cA, cB );
            Assert.That( !r.IsComplete );
            Assert.That( r.HasStructureError && r.StructureErrorCount == 1 );
            ResultChecker.SimpleCheckAndReset( r );
        }
    }

    [Test]
    public void HomonymsByRequires()
    {
        using( TestableItem.IgnoreCheckCount() )
        {
            var item1 = new TestableItem( "A" );
            var item2 = new TestableItem( "A" );
            item2.Requires.Add( item1 );
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, item2, item1 );
                Assert.That( !r.IsComplete );
                Assert.That( r.HasStructureError );
                ResultChecker.SimpleCheckAndReset( r );
            }
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, item1, item2 );
                Assert.That( !r.IsComplete );
                Assert.That( r.HasStructureError );
                ResultChecker.SimpleCheckAndReset( r );
            }
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, item1 );
                Assert.That( r.IsComplete );
                ResultChecker.SimpleCheckAndReset( r );
            }

        }
    }

    [Test]
    public void Container_optional_reference()
    {
        var C = new TestableContainer( "C" );
        var A = new TestableItem( "A" );
        A.Container = new NamedDependentItemContainerRef( "C", true );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, A, C );
            Assert.That( r.IsComplete );
            Assert.That( r.IsOrdered( "C.Head", "A", "C" ) );
            ResultChecker.SimpleCheckAndReset( r );
        }
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, A );
            Assert.That( r.IsComplete );
            Assert.That( r.IsOrdered( "A" ) );
            ResultChecker.SimpleCheckAndReset( r );
        }
    }

    [Test]
    public void Children_optional_reference()
    {
        var C = new TestableContainer( "C" );
        var A = new TestableItem( "A" );
        C.Children.Add( new NamedDependentItemContainerRef( "A", true ) );
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, A, C );
            Assert.That( r.IsComplete );
            Assert.That( r.IsOrdered( "C.Head", "A", "C" ) );
            ResultChecker.SimpleCheckAndReset( r );
        }
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, C );
            Assert.That( r.IsComplete );
            Assert.That( r.IsOrdered( "C.Head", "C" ) );
            ResultChecker.SimpleCheckAndReset( r );
        }
    }

    [Test]
    public void Groups_optional_reference()
    {
        var C = new TestableContainer( "C" );
        var A = new TestableItem( "A" );
        A.Groups.Add( new NamedDependentItemContainerRef( "C", true ) );

        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, A, C );
            Assert.That( r.IsComplete );
            Assert.That( r.IsOrdered( "C.Head", "A", "C" ) );
            ResultChecker.SimpleCheckAndReset( r );
        }
        {
            var r = DependencySorter.OrderItems( TestHelper.Monitor, A );
            Assert.That( r.IsComplete );
            Assert.That( r.IsOrdered( "A" ) );
            ResultChecker.SimpleCheckAndReset( r );
        }
    }
}
