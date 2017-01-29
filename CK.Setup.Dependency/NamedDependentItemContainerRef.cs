#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setup.Dependency\NamedDependentItemContainerRef.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{

    /// <summary>
    /// Implements a named reference to a container.
    /// </summary>
    public class NamedDependentItemContainerRef : NamedDependentItemGroupRef, IDependentItemContainerRef
    {
        /// <summary>
        /// Initializes a new <see cref="NamedDependentItemContainerRef"/> with a <see cref="NamedDependentItemRef.FullName">FullName</see>
        /// optionaly starting with '?'.
        /// </summary>
        public NamedDependentItemContainerRef( string fullName )
            : base( fullName )
        {
        }

        /// <summary>
        /// Initializes a potentially optional new <see cref="NamedDependentItemContainerRef"/> with a <see cref="NamedDependentItemRef.FullName">FullName</see>.
        /// </summary>
        public NamedDependentItemContainerRef( string fullName, bool optional )
            : base( fullName, optional )
        {
        }

        /// <summary>
        /// Returns this instance or creates a new <see cref="NamedDependentItemContainerRef"/> with the given full name if needed.
        /// </summary>
        /// <param name="fullName">New full name.</param>
        /// <returns>This instance or a new one.</returns>
        public new NamedDependentItemContainerRef SetFullName( string fullName )
        {
            return (NamedDependentItemContainerRef)base.SetFullName( fullName );
        }

        /// <summary>
        /// Overriden to create a <see cref="NamedDependentItemContainerRef"/>.
        /// </summary>
        /// <param name="fullName">Full name of the object. May start with '?' but this is ignored: <paramref name="optional"/> drives the optionality.</param>
        /// <param name="optional">True for an optional reference.</param>
        /// <returns>A new <see cref="NamedDependentItemContainerRef"/> instance.</returns>
        protected override NamedDependentItemRef Create( string fullName, bool optional )
        {
            return new NamedDependentItemContainerRef( fullName, optional );
        }
        
        /// <summary>
        /// Implicit conversion from a string.
        /// </summary>
        /// <param name="fullName">The full name of the container.</param>
        /// <returns>A reference to the named container.</returns>
        public static implicit operator NamedDependentItemContainerRef( string fullName )
        {
            return new NamedDependentItemContainerRef( fullName );
        }
    }

}
