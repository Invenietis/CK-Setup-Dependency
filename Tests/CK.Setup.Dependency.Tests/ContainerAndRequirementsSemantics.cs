using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using CK.Core;
using static CK.Testing.MonitorTestHelper;

namespace CK.Setup.Dependency.Tests;

[TestFixture]
public class ContainerAndRequirementsSemantics
{
    [TestCase( true, true )]
    [TestCase( false, true )]
    [TestCase( true, false )]
    [TestCase( false, false )]
    public void Requirements_of_a_Container_are_not_requirements_of_its_items( bool reverseName, bool revertReg )
    {
        using( TestableItem.IgnoreCheckCount() )
        {
            var C = new TestableContainer( "C", "⊐CSub", "∋E", "⇀Item" );
            var CSub = new TestableContainer( "CSub", "∋ESub" );
            var Item = new TestableItem( "Item" );
            var E = new TestableItem( "E" );
            var ESub = new TestableItem( "ESub" );
            IEnumerable<TestableItem> reg = [C, CSub, Item, E, ESub];
            if( revertReg ) reg = reg.Reverse();
            var r = DependencySorter.OrderItems( TestHelper.Monitor, reg, discoverers: null, new DependencySorterOptions { ReverseName = reverseName } );
            Throw.Assert( r.IsComplete );
            GetSorted( "C" ).Requires.ShouldContain( GetSorted( "Item" ) );
            GetSorted( "CSub" ).Requires.ShouldBeEmpty();
            GetSorted( "E" ).Requires.ShouldBeEmpty();
            GetSorted( "ESub" ).Requires.ShouldBeEmpty();

            ISortedItem GetSorted( string name )
            {
                return r.SortedItems.Single( s => s.FullName == name );
            }
        }
    }

    [Test]
    public void ContainerSpecializationLoop()
    {
        using( TestableItem.IgnoreCheckCount() )
        {
            var System = new TestableContainer( "System" );

            // A is a core package (User Management) in System.
            var A = new TestableContainer( "A" );
            A.Container = System;
            // It contains B (Login Management).
            var B = new TestableContainer( "B" );
            B.Container = A;
            // For a project, a specialized User Management is needed.
            var ASpec = new TestableContainer( "ASpec" );
            // ASpec specializes A: 
            // - in term of dependencies, it requires A.
            // - since it specializes A, unless explicitely injected into another container it is also in System.
            ASpec.Requires.Add( A );
            ASpec.Container = System;

            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, A, B, ASpec );
                Throw.Assert( r.IsComplete );
                r.AssertOrdered( "System.Head", "A.Head", "B.Head", "B", "A", "ASpec.Head", "ASpec", "System" );
            }

            // This "Requires me + use my Container" relationship is a kind of "Generalization".
            ASpec.Requires.Clear();
            ASpec.Container = null;
            // Previous scenario is the same as:
            ASpec.Generalization = A;
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, A, B, ASpec );
                Throw.Assert( r.IsComplete );
                r.AssertOrdered( "System.Head", "A.Head", "B.Head", "B", "A", "ASpec.Head", "ASpec", "System" );
            }

            // Note that the Requires set is cleaned up: a Requirement to a Generalization is removed.
            {
                // (This test also uses the Reverse naming.)
                ASpec.Generalization = null;
                ASpec.Requires.Add( A );
                {
                    var r = DependencySorter.OrderItems( TestHelper.Monitor, true, A, B, ASpec );
                    Throw.Assert( r.IsComplete );
                    r.AssertOrdered( "System.Head", "A.Head", "B.Head", "B", "A", "System", "ASpec.Head", "ASpec" );
                    // Here ASpec => A appears.
                    r.SortedItems.Single( s => s.FullName == "ASpec" ).Requires.Single().FullName.ShouldBe( "A" );
                }
                ASpec.Generalization = A;
                {
                    var r = DependencySorter.OrderItems( TestHelper.Monitor, true, A, B, ASpec );
                    Throw.Assert( r.IsComplete );
                    // Even with reverse naming, since ASpec is in System, System container closes the chain.
                    r.AssertOrdered( "System.Head", "A.Head", "B.Head", "B", "A", "ASpec.Head", "ASpec", "System" );
                    r.SortedItems.Single( s => s.FullName == "ASpec" ).Requires.ShouldBeEmpty();
                }
                ASpec.Requires.Clear();
            }


            // John would like ASpec to be inside the package A:
            ASpec.Container = A;
            // It does not work: how a more specialized object can be "contained" in its "Generalization"?
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, A, B, ASpec );
                Throw.Assert( !r.IsComplete );
                r.CycleDetected.ShouldNotBeNull().ShouldNotBeEmpty();
            }
            // Of course, ASpec can not be inside B...
            ASpec.Container = B;
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, A, B, ASpec );
                Throw.Assert( !r.IsComplete );
                r.CycleDetected.ShouldNotBeNull().ShouldNotBeEmpty();
            }
        }
    }


    [Test]
    public void Container_and_requires_are_not_so_easy_to_understand()
    {
        using( TestableItem.IgnoreCheckCount() )
        {
            var C = new TestableContainer( "C" );
            var I = new TestableItem( "I" );

            // Item 'I' cannot be both in 'C' and requires it.
            // This is because a "Requirement" is on the tail of a Container (actually not the Head but 
            // the Container itself).
            C.Children.Add( I );
            I.Requires.Add( C );
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, C );
                Throw.Assert( !r.IsComplete );
                r.CycleExplainedString.ShouldBe( "↳ C ⊐ I ⇀ C" );
            }
            // Under certain circumstances, one can consider that an item that is contained in a Container can require it.
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, new DependencySorterOptions() { SkipDependencyToContainer = true }, C );
                Throw.Assert( r.IsComplete );
                r.IsOrdered( "C.Head", "I", "C" ).ShouldBeTrue();
                r.SortedItems[1].Requires.ShouldBeEmpty( "Requires have been cleaned up." );
            }
            I.Requires.Clear();
            I.Generalization = C;
            // Generalization is not concerned by this option.
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, C );
                Throw.Assert( !r.IsComplete );
                r.CycleExplainedString.ShouldBe( "↳ C ⊐ I ↟ C" );
            }
            // Of course, this works even with Generalization's Container "inheritance".
            var SuperC = new TestableContainer( "SuperC", C );
            var A = new TestableItem( "A", "⊏C" );
            I.Generalization = A;
            var ISpec = new TestableItem( "ISpec", "↟I" );
            // ISpec is a I that is a A: the container of A is inherited => ISpec belongs to C that is in SuperC.
            // If ISpec requires SuperC, this creates a cycle without the option.
            ISpec.Add( "⇀SuperC" );
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, new DependencySorterOptions() { SkipDependencyToContainer = true }, C, ISpec );
                Throw.Assert( r.IsComplete );
                Throw.Assert( r.IsOrdered( "SuperC.Head", "C.Head", "A", "I", "ISpec", "C", "SuperC" ) );
                r.SortedItems[1].Requires.ShouldBeEmpty("Requires have been cleaned up.");
            }
            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, SuperC, ISpec );
                Throw.Assert( !r.IsComplete );
                r.CycleExplainedString.ShouldBe("↳ SuperC ⊐ C ⊐ ISpec ⇀ SuperC");
            }
        }
    }
}
