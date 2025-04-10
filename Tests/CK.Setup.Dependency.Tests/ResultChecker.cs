#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\ResultChecker.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using CK.Core;
using Shouldly;

namespace CK.Setup.Dependency.Tests;

class ResultChecker
{
    Dictionary<string, ISortedItem> _byName;

    public ResultChecker( IDependencySorterResult r )
    {
        r.SortedItems.ShouldNotBeNull();
        Result = r;
        _byName = r.SortedItems.ToDictionary( o => o.FullName );
    }

    public readonly IDependencySorterResult Result;

    public void CheckRecurse( params string[] fullNames )
    {
        foreach( string s in fullNames ) Check( s );
    }

    public static void SimpleCheckAndReset( IDependencySorterResult r )
    {
        if( r.SortedItems != null )
        {
            foreach( var e in r.SortedItems.Where( s => !s.IsGroupHead ).Select( s => s.Item ).OfType<TestableItem>() )
            {
                e.CheckStartDependencySortCountAndReset();
            }
            if( r.SortedItems.Count > 0 )
            {
                var ranks = r.SortedItems.Select( s => s.Rank );
                Throw.Assert( ranks.IsSortedLarge() );
                var distinctRanks = ranks.Distinct().Order().ToList();
                Throw.Assert( distinctRanks.Count == distinctRanks.Last() );
            }
        }
        CheckMissingInvariants( r );
    }

    public static void CheckMissingInvariants( IDependencySorterResult r )
    {
        // Naive implementation. 
        if( r.ItemIssues.Count > 0 )
        {
            Throw.Assert( r.HasRequiredMissing == r.ItemIssues.Any( m => m.RequiredMissingCount > 0 ) );
            foreach( var m in r.ItemIssues )
            {
                int optCount = 0;
                int reqCount = 0;
                foreach( var dep in m.MissingDependencies )
                {
                    if( dep[0] == '?' )
                    {
                        string strong = dep.Substring( 1 );
                        m.MissingDependencies.ShouldNotContain( strong );
                        ++optCount;
                    }
                    else
                    {
                        string weak = '?' + dep;
                        m.MissingDependencies.ShouldNotContain( weak );
                        ++reqCount;
                    }
                }
                Throw.Assert( m.RequiredMissingCount == reqCount );
                Throw.Assert( m.MissingDependencies.Count() == reqCount + optCount );
            }
        }
    }

    void Check( object sortedItemOrFullName )
    {
        ISortedItem? o = sortedItemOrFullName as ISortedItem ?? Find( (string)sortedItemOrFullName );
        if( o == null ) return;

        // If Head, then we check the head/container order and Requires and then we stop.
        if( o.IsGroupHead )
        {
            Throw.Assert( o.Container == o.GroupForHead.Container, "The container is the same for a head and its associated group." );
            Throw.Assert( o.Index < o.GroupForHead.Index, $"{o.FullName} is before {o.GroupForHead.FullName} (since {o.FullName} is the head of {o.GroupForHead.FullName})." );

            // Consider the head as its container (same test as below): the head must be contained in the container of our container if it exists.               
            if( o.Item.Container != null )
            {
                ISortedItem? container = Find( o.Item.Container.FullName );
                Throw.Assert( container != null && container.ItemKind == DependentItemKind.Container );
                CheckItemInContainer( o, container );
            }

            // Requirements of a group is carried by its head.
            CheckRequires( o, o.GroupForHead.Item.Requires.ShouldNotBeNull() );
            return;
        }
        // Checking Generalization.
        if( o.Item.Generalization != null )
        {
            if( !o.Item.Generalization.Optional )
            {
                Throw.Assert( o.Generalization != null && o.Generalization.Item == o.Item.Generalization );
                var gen = _byName[o.Item.Generalization.FullName];
                Throw.Assert( gen.Index < o.Index, $"{gen.FullName} is before {o.FullName} (since {o.FullName} specializes {gen.FullName})." );
            }
        }

        // Checking Container.
        if( o.Item.Container != null )
        {
            ISortedItem? container = Find( o.Item.Container.FullName );
            Throw.Assert( container != null && container.ItemKind == DependentItemKind.Container );
            CheckItemInContainer( o, container );
            // ISortedItem.Requires contains the Requires and the RequiredBy from others.
            CheckRequires( o, o.Requires.Select( r => new NamedDependentItemRef( r.Item.FullName ) ) );
        }

        if( o.ItemKind != DependentItemKind.Item )
        {
            Check( o.HeadForGroup.ShouldNotBeNull() );
            foreach( var item in ((IDependentItemContainer)o.Item).Children.ShouldNotBeNull() ) CheckRecurse( item.FullName );
            // Requirements of a group is carried by its head: we don't check Requires here.
        }
        else CheckRequires( o, o.Item.Requires.ShouldNotBeNull() );

        // RequiredBy applies to normal items and to groups (the container itself, not its head).
        foreach( var invertReq in o.Item.RequiredBy.ShouldNotBeNull() )
        {
            var after = _byName.GetValueOrDefault( invertReq.FullName );
            if( after != null ) Throw.Assert( o.Index < after.Index, $"{o.FullName} is before {after.FullName} (since {after.FullName} is required by {o.FullName})." );
        }
    }

    private void CheckRequires( ISortedItem o, IEnumerable<IDependentItemRef> requirements )
    {
        if( requirements != null )
        {
            foreach( var dep in requirements )
            {
                var before = Find( dep.Optional ? '?' + dep.FullName : dep.FullName );
                if( before != null ) Throw.Assert( before.Index < o.Index, $"{before.FullName} is before {o.FullName} (since {o.FullName} requires {before.FullName})." );
            }
        }
    }

    private static void CheckItemInContainer( ISortedItem o, ISortedItem container )
    {
        Throw.Assert( container != null, "Container necessarily exists." );
        Throw.Assert( container.HeadForGroup.ShouldNotBeNull().Index < o.Index, $"{container.HeadForGroup.FullName} is before {o.FullName} (since {container.HeadForGroup.FullName} contains {o.FullName})." );
        Throw.Assert( o.Index < container.Index, $"{o.FullName} is before {container.FullName} (since {container.FullName} contains {o.FullName})." );
    }

    ISortedItem? Find( string fullNameOpt )
    {
        bool found = _byName.TryGetValue( fullNameOpt, out ISortedItem? o );
        Throw.Assert( found || IsDetectedMissingDependency( fullNameOpt ) );
        return o;
    }

    private bool IsDetectedMissingDependency( string fullName )
    {
        return Result.ItemIssues.Any( m => m.MissingDependencies.Contains( fullName ) );
    }

}
