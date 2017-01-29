#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setup.Dependency\NamedDependentItemRef.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Implements a named reference to an item.
    /// </summary>
    public class NamedDependentItemRef : IDependentItemNamedRef
    {
        readonly string _fullName;
        readonly bool _optional;

        /// <summary>
        /// Initializes a new <see cref="NamedDependentItemRef"/> with a <see cref="FullName"/>
        /// optionaly starting with '?'.
        /// </summary>
        /// <param name="fullName">Full name of the object. May start with '?'.</param>
        public NamedDependentItemRef( string fullName )
        {
            if( String.IsNullOrWhiteSpace( fullName ) ) throw new ArgumentException( "Must not be a not null nor empty nor whitespace string.", "fullName" );
            _fullName = fullName;
            _optional = false;
            if( fullName[0] == '?' )
            {
                _fullName = fullName.Substring( 1 );
                _optional = true;
            }
        }

        /// <summary>
        /// Initializes a potentially optional new <see cref="NamedDependentItemRef"/> with a <see cref="FullName"/>.
        /// </summary>
        /// <param name="fullName">Full name of the object. May start with '?' but this is ignored: <paramref name="optional"/> drives the optionality.</param>
        /// <param name="optional">True for an optional reference.</param>
        public NamedDependentItemRef( string fullName, bool optional )
            : this( fullName )
        {
            _optional = optional;
        }

        /// <summary>
        /// Gets the name that uniquely identifies an item. 
        /// </summary>
        public string FullName 
        {
            get { return _fullName; }
        }

        /// <summary>
        /// Gets whether this is an optional reference.
        /// </summary>
        public bool Optional
        {
            get { return _optional; }
        }

        /// <summary>
        /// Explicit implementation that relays to public covariant <see cref="SetFullName"/> method that itself 
        /// uses protected virtual <see cref="Create"/> to be able to return specialized types.
        /// </summary>
        IDependentItemNamedRef IDependentItemNamedRef.SetFullName( string fullName )
        {
            return SetFullName( fullName );
        }

        /// <summary>
        /// Returns this instance or creates a new <see cref="NamedDependentItemRef"/> (or a more specialized type) with the given full name if needed.
        /// </summary>
        /// <param name="fullName">New full name of the reference.</param>
        /// <returns>This instance or a new one.</returns>
        /// <remarks>
        /// This implementation is not virtual to offer return type covariance.
        /// It calls protected virtual <see cref="Create"/> to be able to return specialized types.
        /// </remarks>
        public NamedDependentItemRef SetFullName( string fullName )
        {
            if( fullName == _fullName ) return this;
            return Create( fullName, _optional );
        }

        /// <summary>
        /// Kind of "virtual constructor" to create an instance of the same type.
        /// </summary>
        /// <param name="fullName">Full name of the object. May start with '?' but this is ignored: <paramref name="optional"/> drives the optionality.</param>
        /// <param name="optional">True for an optional reference.</param>
        /// <returns>A new instance.</returns>
        protected virtual NamedDependentItemRef Create( string fullName, bool optional )
        {
            return new NamedDependentItemRef( fullName, _optional );
        }

        /// <summary>
        /// Implicit conversion from a string.
        /// </summary>
        /// <param name="fullName">The full name of the item.</param>
        /// <returns>A reference to the named item.</returns>
        public static implicit operator NamedDependentItemRef( string fullName )
        {
            return new NamedDependentItemRef( fullName );
        }

        /// <summary>
        /// Overridden to support value semantics.
        /// </summary>
        /// <param name="obj">The object to test.</param>
        /// <returns>True if the object is a <see cref="IDependentItemRef"/> with the same name and optionality.</returns>
        public override bool Equals( object obj )
        {
            if( obj is IDependentItemRef )
            {
                IDependentItemRef o = (IDependentItemRef)obj;
                return o.Optional == Optional && o.FullName == _fullName;
            }
            return false;
        }

        /// <summary>
        /// Overridden to support value semantics.
        /// </summary>
        /// <returns>Hash is based on <see cref="Optional"/> and <see cref="FullName"/>.</returns>
        public override int GetHashCode()
        {
            int h = _fullName.GetHashCode();
            if( _optional ) h = -h;
            return h;
        }

        /// <summary>
        /// Overridden to return the <see cref="FullName"/>.
        /// </summary>
        /// <returns>This <see cref="FullName"/>.</returns>
        public override string ToString()
        {
            return FullName;
        }

    }
}
