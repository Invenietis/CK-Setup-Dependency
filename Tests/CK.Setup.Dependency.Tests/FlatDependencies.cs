#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\FlatDependencies.cs) is part of CK-Database. 
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

namespace CK.Setup.Dependency.Tests
{
    [TestFixture]
    public class FlatDependencies
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void NoItem()
        {
            IDependencySorterResult r = DependencySorter.OrderItems( TestHelper.Monitor, Array.Empty<TestableItem>(), null );
            Assert.That( r.CycleDetected == null );
            Assert.That( r.ItemIssues, Is.Empty );
            Assert.That( r.SortedItems, Is.Empty );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void OneItem()
        {
            var oneItem = new TestableItem( "Test" );
            IDependencySorterResult r = DependencySorter.OrderItems( TestHelper.Monitor, new[] { oneItem }, null );
            Assert.That( r.CycleDetected == null );
            Assert.That( r.ItemIssues, Is.Empty );
            Assert.That( r.SortedItems.Count, Is.EqualTo( 1 ) );
            Assert.That( r.SortedItems[0].Item, Is.SameAs( oneItem ) );
            new ResultChecker( r ).CheckRecurse( "Test" );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void OneItemMissingDependency()
        {
            var oneItem = new TestableItem( "Test", "⇀MissingDep" );
            IDependencySorterResult r = DependencySorter.OrderItems( TestHelper.Monitor, new[] { oneItem }, null );
            Assert.That( r.CycleDetected == null );
            Assert.That( r.HasRequiredMissing );
            Assert.That( r.HasStructureError );

            r.ConsiderRequiredMissingAsStructureError = false;
            Assert.That( r.HasRequiredMissing && r.HasStructureError == false );

            Assert.That( r.ItemIssues.Count, Is.EqualTo( 1 ) );
            Assert.That( r.ItemIssues[0].Item, Is.SameAs( oneItem ) );
            Assert.That( r.ItemIssues.SelectMany( m => m.MissingDependencies ), Is.EquivalentTo( new []{ "MissingDep" } ) );
            ResultChecker.CheckMissingInvariants( r );

            Assert.That( r.SortedItems.Count, Is.EqualTo( 1 ) );
            Assert.That( r.SortedItems[0].Item, Is.SameAs( oneItem ) );

            r.ConsiderRequiredMissingAsStructureError = true;
            new ResultChecker( r ).CheckRecurse( "Test" );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void AutoDiscoverRequiredBy()
        {
            var oneItem = new TestableItem( "Test" );
            oneItem.RequiredBy.Add( new TestableItem( "AutoDiscovered" ) );
            IDependencySorterResult r = DependencySorter.OrderItems( TestHelper.Monitor, new[] { oneItem }, null );
            Assert.That( r.CycleDetected == null );
            Assert.That( r.ItemIssues, Is.Empty );
            Assert.That( r.SortedItems.Count, Is.EqualTo( 2 ) );
            Assert.That( r.SortedItems[0].Item, Is.SameAs( oneItem ) );
            Assert.That( r.SortedItems[1].Item.FullName, Is.EqualTo( "AutoDiscovered" ) );

            new ResultChecker( r ).CheckRecurse( "Test", "AutoDiscovered" );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void AutoDiscoverRequires()
        {
            var oneItem = new TestableItem( "Test" );
            oneItem.Requires.Add( new TestableItem( "AutoDiscovered" ) );
            IDependencySorterResult r = DependencySorter.OrderItems( TestHelper.Monitor, new[] { oneItem }, null );
            Assert.That( r.CycleDetected == null );
            Assert.That( r.ItemIssues, Is.Empty );
            Assert.That( r.SortedItems.Count, Is.EqualTo( 2 ) );
            Assert.That( r.SortedItems[0].Item.FullName, Is.EqualTo( "AutoDiscovered" ) );
            Assert.That( r.SortedItems[1].Item, Is.SameAs( oneItem ) );

            new ResultChecker( r ).CheckRecurse( "Test", "AutoDiscovered" );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void TwoDependencies()
        {
            var i1 = new TestableItem( "Base" );
            var i2 = new TestableItem( "User", "⇀Base" );
            {
                IDependencySorterResult r = DependencySorter.OrderItems( TestHelper.Monitor, i1, i2 );
                Assert.That( r.CycleDetected == null );
                Assert.That( r.ItemIssues, Is.Empty );
                Assert.That( r.SortedItems.Count, Is.EqualTo( 2 ) );
                Assert.That( r.SortedItems[0].Item, Is.SameAs( i1 ) );
                Assert.That( r.SortedItems[1].Item, Is.SameAs( i2 ) );

                new ResultChecker( r ).CheckRecurse( "Base", "User" );
                ResultChecker.SimpleCheck( r );
            }
            {
                // Allowing duplicates (and reversing initial order).
                IDependencySorterResult r = DependencySorter.OrderItems( TestHelper.Monitor, i2, i1, i1, i2 );
                Assert.That( r.CycleDetected == null );
                Assert.That( r.ItemIssues, Is.Empty );
                Assert.That( r.SortedItems.Count, Is.EqualTo( 2 ) );
                Assert.That( r.SortedItems[0].Item, Is.SameAs( i1 ) );
                Assert.That( r.SortedItems[1].Item, Is.SameAs( i2 ) );

                new ResultChecker( r ).CheckRecurse( "Base", "User" );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void DuplicateItemName()
        {
            var i1 = new TestableItem( "Test" );
            var i2 = new TestableItem( "Test" );
            IDependencySorterResult r = DependencySorter.OrderItems( TestHelper.Monitor, i1, i2 );
            Assert.That( r.HasStructureError );
           // Since we start with i1:
            Assert.That( r.ItemIssues[0].Item, Is.SameAs( i1 ) );
            Assert.That( r.ItemIssues[0].Homonyms, Is.EquivalentTo( new []{ i2 } ) );
            
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void FiveFullyDefined()
        {
            var i1 = new TestableItem( "System" );
            var i2 = new TestableItem( "Res", "⇀System" );
            var i3 = new TestableItem( "Actor", "⇀Res" );
            var i4 = new TestableItem( "MCulture", "⇀Res", "⇀Actor" );
            var i5 = new TestableItem( "Appli", "⇀MCulture", "⇀Actor" );
            
            var r = DependencySorter.OrderItems( TestHelper.Monitor, i5, i1, i4, i2, i3 );
            Assert.That( r.CycleDetected == null );
            Assert.That( r.ItemIssues, Is.Empty );
            Assert.That( r.SortedItems.Count, Is.EqualTo( 5 ) );
            Assert.That( r.SortedItems[0].Item, Is.SameAs( i1 ) );
            Assert.That( r.SortedItems[1].Item, Is.SameAs( i2 ) );
            Assert.That( r.SortedItems[2].Item, Is.SameAs( i3 ) );
            Assert.That( r.SortedItems[3].Item, Is.SameAs( i4 ) );
            Assert.That( r.SortedItems[4].Item, Is.SameAs( i5 ) );

            new ResultChecker( r ).CheckRecurse( "System", "Res", "Actor", "MCulture", "Appli" );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void OrderingByNames()
        {
            var i1 = new TestableItem( "System" );
            var i2 = new TestableItem( "Res", "⇀System" );
            var i3 = new TestableItem( "Actor", "⇀Res" );
            var i3Bis = new TestableItem( "Acto", "⇀Res" );
            var i3Ter = new TestableItem( "Act", "⇀Res" );
            var i4 = new TestableItem( "MCulture", "⇀Res", "⇀Actor" );
            var i5 = new TestableItem( "Appli", "⇀MCulture", "⇀Actor" );
            var i2Like = new TestableItem( "JustLikeRes", "⇀System" );
            
            var r = DependencySorter.OrderItems( TestHelper.Monitor, i5, i2Like, i1, i3Ter, i4, i2, i3Bis, i3 );
            Assert.That( r.CycleDetected == null );
            Assert.That( r.ItemIssues, Is.Empty );
            Assert.That( r.SortedItems.Count, Is.EqualTo( 8 ) );

            r.AssertOrdered( "System", "JustLikeRes", "Res", "Act", "Acto", "Actor", "MCulture", "Appli" );
            // "Ordering is deterministic: when 2 dependencies are on the same rank, their lexical order makes the difference." );

            new ResultChecker( r ).CheckRecurse( "System", "Res", "Actor", "Acto", "Act", "MCulture", "Appli", "JustLikeRes" );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void OrderingByNamesReverse()
        {
            var i1 = new TestableItem( "System" );
            var i2 = new TestableItem( "Res", "⇀System" );
            var i3 = new TestableItem( "Actor", "⇀Res" );
            var i3Bis = new TestableItem( "Acto", "⇀Res", "⇀AnAwfulMissingDependency" );
            var i3Ter = new TestableItem( "Act", "⇀Res" );
            var i4 = new TestableItem( "MCulture", "⇀Res", "⇀Actor" );
            var i5 = new TestableItem( "Appli", "⇀MCulture", "⇀Actor", "⇀AnOtherMissingDependency" );
            var i2Like = new TestableItem( "JustLikeRes", "⇀System", "⇀AnAwfulMissingDependency" );
            
            // Reversing lexical ordering is the last (optional) parameter.
            var r = DependencySorter.OrderItems( TestHelper.Monitor, true, i5, i2Like, i1, i3Ter, i4, i2, i3Bis, i3 );
            Assert.That( r.CycleDetected == null );

            // Since we started to add i5, the i5 => AnOtherMissingDependency is the first one, then comes the i2Like and the i3Bis.
            Assert.That( r.ItemIssues.SelectMany( d => d.MissingDependencies ), Is.EquivalentTo( new[] { "AnOtherMissingDependency", "AnAwfulMissingDependency", "AnAwfulMissingDependency" } ) );
            ResultChecker.CheckMissingInvariants( r );
            
            Assert.That( r.SortedItems.Count, Is.EqualTo( 8 ) );
            r.AssertOrdered( "System", "Res", "JustLikeRes", "Actor", "Acto", "Act", "MCulture", "Appli" );
            //Reversing of the order for 2 dependencies that are on the same rank can help detect missing dependencies: 
            //a setup MUST work regardless of the fact that we invert the order of items that have the same rank: since they 
            //share their rank there is NO dependency between them.

            r.ConsiderRequiredMissingAsStructureError = false;
            Assert.That( r.HasRequiredMissing && r.HasStructureError == false );
            r.ConsiderRequiredMissingAsStructureError = true;

            new ResultChecker( r ).CheckRecurse( "System", "Res", "Actor", "Acto", "Act", "MCulture", "Appli", "JustLikeRes" );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void CycleDetection()
        {
            // A => B => C => D => E => F => C
            var a = new TestableItem( "A", "⇀B" );
            var b = new TestableItem( "B", "⇀C" );
            var c = new TestableItem( "C", "⇀D" );
            var d = new TestableItem( "D", "⇀E" );
            var e = new TestableItem( "E", "⇀F" );
            var f = new TestableItem( "F", "⇀C" );
            var r = DependencySorter.OrderItems( TestHelper.Monitor, e, b, c, d, f, a );
            Assert.That( r.SortedItems, Is.Null );
            Assert.That( r.CycleDetected[0].Item, Is.SameAs( r.CycleDetected.Last().Item ), "Detected cycle shares its first and last item." );           
            Assert.That( r.CycleDetected.Skip(1).Select( ec => ec.Item ), Is.EquivalentTo( new[] { c, d, e, f } ), "Cycle is detected in its entirety: the 'head' can be any participant." );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void CycleDetectionAutoReference()
        {
            // A => B => C => C,D
            var a = new TestableItem( "A", "⇀B" );
            var b = new TestableItem( "B", "⇀C" );
            var c = new TestableItem( "C", "⇀C", "⇀D" );
            var d = new TestableItem( "D" );
            var r = DependencySorter.OrderItems( TestHelper.Monitor, b, c, d, a );
            Assert.That( r.SortedItems, Is.Null );
            Assert.That( r.CycleDetected[0].Item, Is.SameAs( r.CycleDetected.Last().Item ), "Detected cycle shares its first and last item: this is always true (even if there is only one participant)." );
            Assert.That( r.CycleDetected.Count, Is.EqualTo( 2 ), "Cycle is 'c=>c'" );
            Assert.That( r.CycleDetected[0].Item, Is.SameAs( c ), "The culprit is actually the only item." );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void DeferredRequiredByRegistration()
        {
            // This triggers a deferred registration of a RequiredBy object
            // (and this is a case that must be tested).
            var a = new TestableItem( "A" );
            var d = new TestableItem( "D", "⇀A" );
            var e = new TestableItem( "E", "↽D" );
            var r = DependencySorter.OrderItems( TestHelper.Monitor, e, d, a );
            r.AssertOrdered( "A", "E", "D" );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void RequiredBy()
        {
            // a
            // b d 
            // c f h i
            // e
            // g
            var a = new TestableItem( "A" );
            var b = new TestableItem( "B", "⇀A" );
            var c = new TestableItem( "C", "⇀B" );
            var d = new TestableItem( "D", "⇀A" );
            var e = new TestableItem( "E", "⇀C" );
            var f = new TestableItem( "F", "⇀B" );
            var g = new TestableItem( "G", "⇀E" );
            var h = new TestableItem( "H", "⇀B" );
            var i = new TestableItem( "I", "⇀D" );

            var r = DependencySorter.OrderItems( TestHelper.Monitor, e, g, b, h, c, d, i, f, a );
            r.AssertOrdered( "A", "B", "D", "C", "F", "H", "I", "E", "G" );
            ResultChecker.SimpleCheck( r );

            // Now, makes D requires E: D => A,E=>(C=>(B=>(A))) (5), where G => E=>(C=>(B=>(A))) (4)
            // G & D have no dependencies between them and actually share the same rank: the lexical order applies.
            // The last one will be I since I => D
            // a
            // b  
            // c f h
            // e
            // g
            // d
            // i
            e.Add( "↽ D" );
            r = DependencySorter.OrderItems( TestHelper.Monitor, e, c, b, g, h, i, d, f, a );
            r.AssertOrdered( "A", "B", "C", "F", "H", "E", "D", "G", "I" );
            ResultChecker.SimpleCheck( r );

            // This does not change the dependency order per se (it just contributes to make D "heavier" but do not change its rank).
            h.Add( "↽ D" );
            r = DependencySorter.OrderItems( TestHelper.Monitor, f, i, b, g, h, d, e, a, c );
            r.AssertOrdered( "A", "B", "C", "F", "H", "E", "D", "G", "I" );
            ResultChecker.SimpleCheck( r );

            // Missing "RequiredBy" are just useless: we simply forget them (and they do not change anything in the ordering of course).
            // We do not consider them as "Missing Dependencies" since they are NOT missing dependencies :-).
            a.Add( "↽KExistePas", "↽DuTout" );
            b.Add( "↽ KExistePas" );
            r = DependencySorter.OrderItems( TestHelper.Monitor, f, b, h, i, e, g, a, d, c );
            r.AssertOrdered( "A", "B", "C", "F", "H", "E", "D", "G", "I" );
            Assert.That( r.ItemIssues, Is.Empty );
            ResultChecker.SimpleCheck( r );

            // Of course, RequiredBy participates to cycle...
            // B => D => (E, H => B) => C => (B, H => B)
            // Here we created 3 cycles: 
            //  - B => D => H => B
            //  - B => D => E => C => B
            //  - B => D => E => C => H => B
            Assert.That( d.RequiredBy, Is.Null.Or.Empty, "Otherwise this test will fail :-)." );
            d.Add( "↽B" );
            r = DependencySorter.OrderItems( TestHelper.Monitor, f, b, h, i, e, g, a, d, c );
            Assert.That( r.SortedItems, Is.Null );
            Assert.That( r.CycleDetected[0].Item, Is.SameAs( r.CycleDetected.Last().Item ), "Detected cycle shares its first and last item." );

            IEnumerable<IDependentItem> cycleTail = r.CycleDetected.Skip(1).Select(ec => ec.Item);
            bool cycle1 = new[] { d, h, b }.SequenceEqual(cycleTail);
            bool cycle2 = new[] { d, e, c, b }.SequenceEqual(cycleTail);
            bool cycle3 = new[] { d, e, c, h, b }.SequenceEqual(cycleTail);
            Assert.That( cycle1 || cycle2 || cycle3 );

            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void RelatedItems()
        {
            var i1 = new TestableItem( "I1" );
            var i2 = new TestableItem( "I2" );
            var i3 = new TestableItem( "I3" );
            // Auto reference:
            i1.RelatedItems.Add( i1 );
            i1.RelatedItems.Add( i2 );
            i1.RelatedItems.Add( i3 );

            var i4 = new TestableItem( "I4" );
            var i5 = new TestableItem( "I5" );
            var i6 = new TestableItem( "I6" );

            i2.RelatedItems.Add( i4 );
            i4.RelatedItems.Add( i5 );
            i5.RelatedItems.Add( i6 );
            // Back to i2.
            i6.RelatedItems.Add( i2 );

            {
                var r = DependencySorter.OrderItems( TestHelper.Monitor, i1 );
                Assert.That( r.HasStructureError, Is.False );
                Assert.That( r.IsComplete, Is.True );
                Assert.That( r.SortedItems.Count, Is.EqualTo( 6 ) );
                ResultChecker.SimpleCheck( r );
            }

        }

    }
}
