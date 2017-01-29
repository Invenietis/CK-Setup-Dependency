#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\Generalization.cs) is part of CK-Database. 
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
    public class Generalization
    {
        [Test]
        public void OneGeneralization()
        {
            using( TestableItem.IgnoreCheckCount() )
            {
                var A = new TestableItem( "A" );
                var ASpec = new TestableItem( "ASpec" );
                ASpec.Generalization = A;
                {
                    // "A" must be automatically discovered.
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, ASpec );
                    r.AssertOrdered( "A", "ASpec" );
                }
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, true, ASpec );
                    r.AssertOrdered( "A", "ASpec" );
                }

                var container = new TestableContainer( "Container" );
                // First, registers the container that contains A.
                container.Children.Add( A );
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, ASpec, container );
                    r.AssertOrdered( "Container.Head", "A", "ASpec", "Container" );
                    Assert.That( r.SortedItems[2].Container.FullName, Is.EqualTo( "Container" ), "ASpec.Container has been set to A.Container." );
                }
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, true, ASpec, container );
                    r.AssertOrdered( "Container.Head", "A", "ASpec", "Container" );
                    Assert.That( r.Find( "ASpec" ).Container.FullName, Is.EqualTo( "Container" ), "ASpec.Container has been set to A.Container." );
                }
                // Second, register ASpec that generalizes A that references the container: the container is automatically discovered.
                container.Children.Clear();
                A.Container = container;
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, ASpec );
                    r.AssertOrdered( "Container.Head", "A", "ASpec", "Container" );
                    Assert.That( r.Find( "ASpec" ).Container.FullName, Is.EqualTo( "Container" ), "ASpec.Container has been set to A.Container." );
                }
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, true, ASpec );
                    r.AssertOrdered( "Container.Head", "A", "ASpec", "Container" );
                    Assert.That( r.Find( "ASpec" ).Container.FullName, Is.EqualTo( "Container" ), "ASpec.Container has been set to A.Container." );
                }
                // Third, register the container that references A by name.
                container.Add( "⊐A" );
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, ASpec, container );
                    r.AssertOrdered( "Container.Head", "A", "ASpec", "Container" );
                    Assert.That( r.Find( "ASpec" ).Container.FullName, Is.EqualTo( "Container" ), "ASpec.Container has been set to A.Container." );
                }
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, true, ASpec, container );
                    r.AssertOrdered( "Container.Head", "A", "ASpec", "Container" );
                    Assert.That( r.Find( "ASpec" ).Container.FullName, Is.EqualTo( "Container" ), "ASpec.Container has been set to A.Container." );
                }
                // Fourth, register A that reference the container by name.
                container.Children.Clear();
                A.Container = new NamedDependentItemContainerRef( "Container" );
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, ASpec, container );
                    r.AssertOrdered( "Container.Head", "A", "ASpec", "Container" );
                    Assert.That( r.Find( "ASpec" ).Container.FullName, Is.EqualTo( "Container" ), "ASpec.Container has been set to A.Container." );
                }
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, true, ASpec, container );
                    r.AssertOrdered( "Container.Head", "A", "ASpec", "Container" );
                    Assert.That( r.Find( "ASpec" ).Container.FullName, Is.EqualTo( "Container" ), "ASpec.Container has been set to A.Container." );
                }
            }
        }

        [Test]
        public void OneGeneralizationByName()
        {
            using( TestableItem.IgnoreCheckCount() )
            {
                var A = new TestableItem( "A" );
                var ASpec = new TestableItem( "ASpec", "↟A" );
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, ASpec, A );
                    r.AssertOrdered( "A", "ASpec" );
                }
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, true, ASpec, A );
                    r.AssertOrdered( "A", "ASpec" );
                }

                var container = new TestableContainer( "Container" );
                container.Add( A );
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, ASpec, container );
                    r.AssertOrdered( "Container.Head", "A", "ASpec", "Container" );
                    Assert.That( r.SortedItems[2].Container.FullName, Is.EqualTo( "Container" ) );
                }
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, true, ASpec, container );
                    r.AssertOrdered( "Container.Head", "A", "ASpec", "Container" );
                    Assert.That( r.SortedItems[2].Container.FullName, Is.EqualTo( "Container" ) );
                }
            }
        }

        [Test]
        public void CycleDetectionByName0()
        {
            // Ruby belongs to "Root" container.
            // Here we can see the the "Generalized By" ↟ relations.
            var c = new TestableContainer( "Root",
                        new TestableContainer( "Pierre", new TestableItem( "Gem" ) ),
                        new TestableItem( "Nuage", "⇀Rubis", "↽Pierre" ),
                        new TestableItem( "Rubis", "↟Gem" )
                    );
            var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, c );
            Assert.That( r.CycleDetected, Is.Not.Null );
            Assert.That( r.SortedItems, Is.Null );
            Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ Rubis ↟ Gem ⊏ Pierre ⇌ Nuage ⇀ Rubis" ) );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void CycleDetectionByName2()
        {
            using( TestableItem.IgnoreCheckCount() )
            {
                var root = new TestableContainer( "Root",
                                new TestableContainer( "Pierre",
                                    new TestableItem( "Gem" )
                                    ),
                                new TestableContainer( "Nuage", "⇀Pierre",
                                    new TestableItem( "Cumulus" ),
                                    new TestableItem( "Stratus", "↽ Rubis" )
                                    )
                        );
                var rubis = new TestableItem( "Rubis" );

                {
                    // Here: Rubis => Stratus ⊏ Cumulus ⊏ Nuage => Pierre ∋ Gem.
                    //       Rubis is not in a Container. There is no cycle.
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, root, rubis );
                    Assert.That( r.CycleDetected, Is.Null );
                    Assert.That( r.SortedItems, Is.Not.Null );
                    ResultChecker.SimpleCheck( r );
                }
                // Before saying that "Gem" generalizes "Rubis", we check that
                // adding a "Rubis" => "Gem" dependency does not create any cycle.
                rubis.Add( "⇀Gem" );
                {
                    // Here: Rubis => Stratus ⊏ Cumulus ⊏ Nuage => Pierre ∋ Gem, and Rubis => Gem.
                    // No Cycle.
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, root, rubis );
                    Assert.That( r.CycleDetected, Is.Null );
                    Assert.That( r.SortedItems, Is.Not.Null );
                    ResultChecker.SimpleCheck( r );
                }
                // Now we say that "Gem" generalizes "Rubis".
                // This is more than adding "Rubis" => "Gem" dependency because since
                // Rubis has no defined container, Gem's container (Pierre) has been inherited.
                rubis.Requires.Clear();
                rubis.Add( "↟Gem" );
                {
                    // This is like adding Pierre ⊏ Rubis... and this creates a cycle at the Container level:
                    // Rubis => Stratus ⊏ Cumulus ⊏ Nuage => Pierre ⊏ Rubis.
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, root, rubis );
                    Assert.That( r.CycleDetected, Is.Not.Null );
                    Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ Nuage ⇀ Pierre ⊐ Rubis ⇌ Stratus ⊏ Nuage" ) );
                    ResultChecker.SimpleCheck( r );
                }
                // Setting a Container for Rubis (Root for instance), solves the problem.
                rubis.Container = root;
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, root, rubis );
                    Assert.That( r.CycleDetected, Is.Null );
                    Assert.That( r.SortedItems, Is.Not.Null );
                    ResultChecker.SimpleCheck( r );
                }
                // Setting a brand new Container is ok also.
                rubis.Container = new TestableContainer( "Specialized Features." );
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, root, rubis );
                    Assert.That( r.CycleDetected, Is.Null );
                    Assert.That( r.SortedItems, Is.Not.Null );
                    ResultChecker.SimpleCheck( r );
                }
            }
        }

        [Test]
        public void GeneralizationIsOptional()
        {
            using( TestableItem.IgnoreCheckCount() )
            {
                var A = new TestableItem( "A" );
                var ASpec = new TestableItem( "ASpec" );
                ASpec.Generalization = new NamedDependentItemRef( "?A" );

                // When A is here, nothing changed...
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, ASpec, A );
                    r.AssertOrdered( "A", "ASpec" );
                    Assert.That( r.SortedItems[1].Generalization, Is.SameAs( r.SortedItems[0] ) );
                }
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, true, ASpec, A );
                    r.AssertOrdered( "A", "ASpec" );
                    Assert.That( r.SortedItems[1].Generalization, Is.SameAs( r.SortedItems[0] ) );
                }
                // When A is NOT here it is okay...
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, ASpec );
                    r.AssertOrdered( "ASpec" );
                    Assert.That( r.SortedItems[0].Generalization, Is.Null );
                }
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, true, ASpec );
                    r.AssertOrdered( "ASpec" );
                    Assert.That( r.SortedItems[0].Generalization, Is.Null );
                }
            }
        }

        [Test]
        public void OptionalGeneralizationIsNOTAutomaticallyDiscovered()
        {
            using( TestableItem.IgnoreCheckCount() )
            {
                var A = new TestableItem( "A" );
                var ASpec = new TestableItem( "ASpec" );
                ASpec.Generalization = ((IDependentItem)A).GetOptionalReference();

                // Optional reference to an otpional Generalization is automatically discovered...
                {
                    var r = DependencySorter.OrderItems( TestHelper.ConsoleMonitor, ASpec );
                    r.AssertOrdered( "ASpec" );
                    Assert.That( r.SortedItems[0].Generalization, Is.Null );
                }
            }
        }


    }
}
