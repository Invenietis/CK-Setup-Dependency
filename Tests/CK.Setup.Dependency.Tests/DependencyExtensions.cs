#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\DependencyExtensions.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;

namespace CK.Setup.Dependency.Tests
{
    static class DependencyExtensions
    {
        public static IEnumerable<string> OrderedFullNames( this IDependencySorterResult @this )
        {
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
                Assert.Fail( "Expecting '{0}' but was '{1}'.", String.Join( ", ", fullNames ), String.Join( ", ", OrderedFullNames( @this ) ) );
            }
        }

        public static void CheckChildren( this IDependencySorterResult @this, string fullName, string childrenFullNames )
        {
            Check( @this, Find( @this, fullName ).Children, childrenFullNames );
        }

        public static void Check( this IDependencySorterResult @this, IEnumerable<ISortedItem> items, string fullNames )
        {
            var s1 = items.Select( i => i.FullName ).OrderBy( Util.FuncIdentity );
            var s2 = fullNames.Split( ',' ).OrderBy( Util.FuncIdentity );
            if( !s1.SequenceEqual( s2 ) )
            {
                Assert.Fail( "Expecting '{0}' but was '{1}'.", String.Join( ", ", s2 ), String.Join( ", ", s1 ) );
            }
        }
        
        public static ISortedItem Find( this IDependencySorterResult @this, string fullName )
        {
            return @this.SortedItems.FirstOrDefault( i => i.FullName == fullName );
        }
    }
}
