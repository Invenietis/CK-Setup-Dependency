#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\ResultChecker.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using NUnit.Framework;

namespace CK.Setup.Dependency.Tests
{
    class ResultChecker
    {
        Dictionary<string, ISortedItem> _byName;

        public ResultChecker( IDependencySorterResult r )
        {
            Result = r;
            _byName = r.SortedItems.ToDictionary( o => o.FullName );
        }
        
        public readonly IDependencySorterResult Result;

        public void CheckRecurse( params string[] fullNames )
        {
            foreach( string s in fullNames ) Check( s );
        }

        public static void SimpleCheck( IDependencySorterResult r )
        {
            if( r.SortedItems != null )
            {
                foreach( var e in r.SortedItems.Where( s => !s.IsGroupHead ).Select( s => s.Item ).OfType<TestableItem>() )
                {
                    e.CheckStartDependencySortCountAndReset();
                }
            }
            CheckMissingInvariants( r );
        }

        public static void CheckMissingInvariants( IDependencySorterResult r )
        {
            // Naive implementation. 
            if( r.ItemIssues.Count > 0 )
            {
                Assert.That( r.HasRequiredMissing == r.ItemIssues.Any( m => m.RequiredMissingCount > 0 ) );
                foreach( var m in r.ItemIssues )
                {
                    int optCount = 0;
                    int reqCount = 0;
                    foreach( var dep in m.MissingDependencies )
                    {
                        if( dep[0] == '?' )
                        {
                            string strong = dep.Substring( 1 );
                            Assert.That( m.MissingDependencies.Contains( strong ), Is.False ); 
                            ++optCount;
                        }
                        else
                        {
                            string weak = '?' + dep;
                            Assert.That( m.MissingDependencies.Contains( weak ), Is.False );
                            ++reqCount;
                        }
                    }
                    Assert.That( m.RequiredMissingCount == reqCount );
                    Assert.That( m.MissingDependencies.Count() == reqCount + optCount );
                }
            }
        }

        void Check( object sortedItemOrFullName )       
        {
            ISortedItem o = sortedItemOrFullName is ISortedItem ? (ISortedItem)sortedItemOrFullName : Find( (string)sortedItemOrFullName );
            if( o == null ) return;

            // If Head, then we check the head/container order and Requires and then we stop.
            if( o.IsGroupHead )
            {
                Assert.That( o.Container == o.GroupForHead.Container, "The container is the same for a head and its associated group." );
                Assert.That( o.Index < o.GroupForHead.Index, "{0} is before {1} (since {0} is the head of {1}).", o.FullName, o.GroupForHead.FullName );
                
                // Consider the head as its container (same test as below): the head must be contained in the container of our container if it exists.               
                if( o.Item.Container != null )
                {
                    ISortedItem container = Find( o.Item.Container.FullName );
                    Assert.That( container != null && container.ItemKind == DependentItemKind.Container );
                    CheckItemInContainer( o, container );
                }

                // Requirements of a group is carried by its head.
                CheckRequires( o, o.GroupForHead.Item.Requires );
                return;
            }
            // Checking Generalization.
            if( o.Item.Generalization != null )
            {
                if( !o.Item.Generalization.Optional )
                {
                    Assert.That( o.Generalization != null && o.Generalization.Item == o.Item.Generalization );
                    var gen = _byName[ o.Item.Generalization.FullName ];
                    Assert.That( gen.Index < o.Index, "{0} is before {1} (since {1} specializes {0}).", gen.FullName, o.FullName );
                }
            }

            // Checking Container.
            if( o.Item.Container != null )
            {
                ISortedItem container = Find( o.Item.Container.FullName );
                Assert.That( container != null && container.ItemKind == DependentItemKind.Container );
                CheckItemInContainer( o, container );
            }
            
            if( o.ItemKind != DependentItemKind.Item )
            {
                Check( o.HeadForGroup );
                foreach( var item in ((IDependentItemContainer)o.Item).Children ) CheckRecurse( item.FullName );
                // Requirements of a group is carried by its head: we don't check Requires here.
            }
            else CheckRequires( o, o.Item.Requires );
            
            // RequiredBy applies to normal items and to groups (the container itself, not its head).
            foreach( var invertReq in o.Item.RequiredBy )
            {
                var after = _byName.GetValueWithDefault( invertReq.FullName, null );
                if( after != null ) Assert.That( o.Index < after.Index, "{0} is before {1} (since {1} is required by {0}).", o.FullName, after.FullName );
            }
        }

        private void CheckRequires( ISortedItem o, IEnumerable<IDependentItemRef> requirements )
        {
            if( requirements != null )
            {
                foreach( var dep in requirements )
                {
                    var before = Find( dep.Optional ? '?' + dep.FullName : dep.FullName );
                    if( before != null ) Assert.That( before.Index < o.Index, "{0} is before {1} (since {1} requires {0}).", before.FullName, o.FullName );
                }
            }
        }

        private static void CheckItemInContainer( ISortedItem o, ISortedItem container )
        {
            Assert.That( container != null, "Container necessarily exists." );
            Assert.That( container.HeadForGroup.Index < o.Index, "{0} is before {1} (since {0} contains {1}).", container.HeadForGroup.FullName, o.FullName );
            Assert.That( o.Index < container.Index, "{0} is before {1} (since {1} contains {0}).", o.FullName, container.FullName );
        }

        ISortedItem Find( string fullNameOpt )
        {
            ISortedItem o;
            bool found = _byName.TryGetValue( fullNameOpt, out o );
            Assert.That( found || IsDetectedMissingDependency( fullNameOpt ), "{0} not found.", fullNameOpt );
            return o;
        }

        private bool IsDetectedMissingDependency( string fullName )
        {
            return Result.ItemIssues.Any( m => m.MissingDependencies.Contains( fullName ) );
        }

    }
}
