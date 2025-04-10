#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\DependencyExtensions.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CK.Core;

namespace CK.Setup.Dependency.Tests;

static class DependencyExtensions
{
    public static IEnumerable<string> OrderedFullNames( this IDependencySorterResult @this )
    {
        Throw.Assert( @this.SortedItems != null );
        return @this.SortedItems.Select( o => o.FullName );
    }

    public static bool IsOrdered( this IDependencySorterResult @this, params string[] fullNames )
    {
        return OrderedFullNames( @this ).SequenceEqual( fullNames );
    }

    public static void AssertOrdered( this IDependencySorterResult @this, params string[] fullNames )
    {
        if( !OrderedFullNames( @this ).SequenceEqual( fullNames ) )
        {
            Assert.Fail( $"Expecting '{String.Join( ", ", fullNames )}' but was '{String.Join( ", ", OrderedFullNames( @this ) )}'." );
        }
    }

    public static void CheckChildren( this IDependencySorterResult @this, string fullName, string childrenFullNames )
    {
        Check( @this, Find( @this, fullName )!.Children, childrenFullNames );
        // AllChildren in the current tests are always the same as Children.
        // If a new test (that should be done, btw), breaks this, this should be rewritten.
        Check( @this, Find( @this, fullName )!.GetAllChildren(), childrenFullNames );
    }

    public static void Check( this IDependencySorterResult @this, IEnumerable<ISortedItem> items, string fullNames )
    {
        var s1 = items.Select( i => i.FullName ).OrderBy( Util.FuncIdentity );
        var s2 = fullNames.Split( ',' ).OrderBy( Util.FuncIdentity );
        if( !s1.SequenceEqual( s2 ) )
        {
            Assert.Fail( $"Expecting '{String.Join( ", ", s2 )}' but was '{String.Join( ", ", s1 )}'." );
        }
    }

    public static ISortedItem? Find( this IDependencySorterResult @this, string fullName )
    {
        Throw.Assert( @this.SortedItems != null );
        return @this.SortedItems.FirstOrDefault( i => i.FullName == fullName );
    }
}
