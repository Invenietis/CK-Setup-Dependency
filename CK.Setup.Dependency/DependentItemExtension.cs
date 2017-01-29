#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setup.Dependency\DependentItemExtension.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{

    /// <summary>
    /// Exposes extension methods on <see cref="IDependentItem"/> and <see cref="IDependentItemContainer"/>.
    /// </summary>
    public static partial class DependentItemExtension
    {
        /// <summary>
        /// Returns a collection of <see cref="IDependentItemRef"/> with potentially different <see cref="IDependentItem.FullName"/>.
        /// Can safely be called on null source reference.
        /// </summary>
        /// <param name="this">This enumerable.</param>
        /// <param name="fullNameProvider">Name provider. By returning null, the reference does not appear in the resulting enumerable.</param>
        /// <returns>A collection of <see cref="IDependentItemRef"/> whose FullName of named references may have changed.</returns>
        public static IEnumerable<IDependentItemRef> SetRefFullName( this IEnumerable<IDependentItemRef> @this, Func<IDependentItemNamedRef, string> fullNameProvider )
        {
            return @this != null ? CallSetRefFullName( @this, fullNameProvider ) : @this;
        }

        /// <summary>
        /// Returns a collection of <see cref="IDependentItemGroupRef"/> with potentially different <see cref="IDependentItem.FullName"/>.
        /// Can safely be called on null source reference.
        /// </summary>
        /// <param name="this">This enumerable.</param>
        /// <param name="fullNameProvider">Name provider. By returning null, the reference does not appear in the resulting enumerable.</param>
        /// <returns>A collection of <see cref="IDependentItemGroupRef"/> whose FullName of named references may have changed.</returns>
        public static IEnumerable<IDependentItemGroupRef> SetRefFullName( this IEnumerable<IDependentItemGroupRef> @this, Func<IDependentItemNamedRef, string> fullNameProvider )
        {
            return @this != null ? CallSetRefFullName( @this, fullNameProvider ) : @this;
        }

        /// <summary>
        /// Returns a collection of <see cref="IDependentItemContainerRef"/> with potentially different <see cref="IDependentItem.FullName"/>.
        /// Can safely be called on null source reference.
        /// </summary>
        /// <param name="this">This enumerable.</param>
        /// <param name="fullNameProvider">Name provider. By returning null, the reference does not appear in the resulting enumerable.</param>
        /// <returns>A collection of <see cref="IDependentItemContainerRef"/> whose FullName of named references may have changed.</returns>
        public static IEnumerable<IDependentItemContainerRef> SetRefFullName( this IEnumerable<IDependentItemContainerRef> @this, Func<IDependentItemNamedRef,string> fullNameProvider )
        {
            return @this != null ? CallSetRefFullName( @this, fullNameProvider ) : @this;
        }

        /// <summary>
        /// Returns an updated reference if this is a <see cref="IDependentItemNamedRef"/>.
        /// Can safely be called on null reference and on reference that are not <see cref="IDependentItemNamedRef"/> (this unifies the API of extension 
        /// methods SetRefFullName on <see cref="IEnumerable{T}"/> of <see cref="IDependentItem"/> and its specializations).
        /// </summary>
        /// <param name="this">This item reference.</param>
        /// <param name="fullNameProvider">Name provider. When null is returned, it is ignored.</param>
        /// <returns>The reference with an updated name.</returns>
        public static IDependentItemRef SetRefFullName( this IDependentItemRef @this, Func<IDependentItemNamedRef, string> fullNameProvider )
        {
            IDependentItemNamedRef r = @this as IDependentItemNamedRef;
            return r != null ? (IDependentItemRef)r.SetFullName( fullNameProvider( r ) ) : @this;
        }

        /// <summary>
        /// Returns an updated reference if this is a <see cref="IDependentItemNamedRef"/>.
        /// Can safely be called on null reference and on reference that are not <see cref="IDependentItemNamedRef"/> (this unifies the API of extension 
        /// methods SetRefFullName on <see cref="IEnumerable{T}"/> of <see cref="IDependentItem"/> and its specializations).
        /// </summary>
        /// <param name="this">This group reference.</param>
        /// <param name="fullNameProvider">Name provider. When null is returned, it is ignored.</param>
        /// <returns>The reference with an updated name.</returns>
        public static IDependentItemGroupRef SetRefFullName( this IDependentItemGroupRef @this, Func<IDependentItemNamedRef, string> fullNameProvider )
        {
            IDependentItemNamedRef r = @this as IDependentItemNamedRef;
            return r != null ? (IDependentItemGroupRef)r.SetFullName( fullNameProvider( r ) ) : @this;
        }

        /// <summary>
        /// Returns an updated reference if this is a <see cref="IDependentItemNamedRef"/>.
        /// Can safely be called on null reference and on reference that are not <see cref="IDependentItemNamedRef"/> (this unifies the API of extension 
        /// methods SetRefFullName on <see cref="IEnumerable{T}"/> of <see cref="IDependentItem"/> and its specializations).
        /// </summary>
        /// <param name="this">This container reference.</param>
        /// <param name="fullNameProvider">Name provider. When null is returned, it is ignored.</param>
        /// <returns>The reference with an updated name.</returns>
        public static IDependentItemContainerRef SetRefFullName( this IDependentItemContainerRef @this, Func<IDependentItemNamedRef, string> fullNameProvider )
        {
            IDependentItemNamedRef r = @this as IDependentItemNamedRef;
            return r != null ? (IDependentItemContainerRef)r.SetFullName( fullNameProvider( r ) ) : @this;
        }

        /// <summary>
        /// Private implementation to avoid duplicated code or IEnumerable{T} extension methods pollution.
        /// </summary>
        static IEnumerable<T> CallSetRefFullName<T>( this IEnumerable<T> source, Func<IDependentItemNamedRef, string> fullNameProvider )
        {
            foreach( var e in source )
            {
                IDependentItemNamedRef named = e as IDependentItemNamedRef;
                if( named != null )
                {
                    string newName = fullNameProvider( named );
                    if( newName != null )
                    {
                        if( newName == named.FullName ) yield return e;
                        else yield return (T)named.SetFullName( newName );
                    }
                }
                else yield return e;
            }
        }

        /// <summary>
        /// Private base class that hosts a reference to a dependent items.
        /// </summary>
        class DirectRef
        {
            public IDependentItem Item { get; protected set; }
        }

        class ItemRef : DirectRef, IDependentItemContainerRef
        {
            public ItemRef( IDependentItem item )
            {
                Item = item;
            }

            public string FullName
            {
                get { return Item.FullName; }
            }

            public bool Optional
            {
                get { return false; }
            }

            public override bool Equals( object obj )
            {
                if( obj is IDependentItemRef )
                {
                    IDependentItemRef o = (IDependentItemRef)obj;
                    return !o.Optional && o.FullName == Item.FullName;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return Item.FullName.GetHashCode();
            }
        }

        class OptItemRef : DirectRef, IDependentItemContainerRef
        {
            public OptItemRef( IDependentItem item )
            {
                Item = item;
            }

            public string FullName
            {
                get { return Item.FullName; }
            }

            public bool Optional
            {
                get { return true; }
            }

            public override bool Equals( object obj )
            {
                if( obj is IDependentItemRef )
                {
                    IDependentItemRef o = (IDependentItemRef)obj;
                    return Optional && o.FullName == Item.FullName;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return -Item.FullName.GetHashCode();
            }
        }

        #region GetReference and GetOptionalReference on IDependentItem, IDependentItemGroup and IDependentItemContainer

        /// <summary>
        /// Obtains an <see cref="IDependentItemRef"/> for a <see cref="IDependentItem"/> (if <paramref name="this"/> is already a reference, it is returned as-is).
        /// </summary>
        /// <param name="this">This <see cref="IDependentItem"/>. Can be null.</param>
        /// <returns>A reference to the item. Null if <paramref name="this"/> is null.</returns>
        public static IDependentItemRef GetReference( this IDependentItem @this )
        {
            IDependentItemRef r = @this as IDependentItemRef;
            if( r == null ) r = @this != null ? new ItemRef( @this ) : null;
            return r;
        }

        /// <summary>
        /// Obtains an <see cref="IDependentItemGroupRef"/> for a <see cref="IDependentItemGroup"/> (if <paramref name="this"/> is already a reference, it is returned as-is).
        /// </summary>
        /// <param name="this">This <see cref="IDependentItemGroup"/>. Can be null.</param>
        /// <returns>A reference to the group. Null if <paramref name="this"/> is null.</returns>
        public static IDependentItemGroupRef GetReference( this IDependentItemGroup @this )
        {
            IDependentItemGroupRef r = @this as IDependentItemGroupRef;
            if( r == null ) r = @this != null ? new ItemRef( @this ) : null;
            return r;
        }

        /// <summary>
        /// Obtains an <see cref="IDependentItemContainerRef"/> for a <see cref="IDependentItemContainer"/> (if <paramref name="this"/> is already a reference, it is returned as-is).
        /// </summary>
        /// <param name="this">This <see cref="IDependentItemContainer"/>. Can be null.</param>
        /// <returns>A reference to the container. Null if <paramref name="this"/> is null.</returns>
        public static IDependentItemContainerRef GetReference( this IDependentItemContainer @this )
        {
            IDependentItemContainerRef r = @this as IDependentItemContainerRef;
            if( r == null ) r = @this != null ? new ItemRef( @this ) : null;
            return r;
        }

        /// <summary>
        /// Obtains an <see cref="IDependentItemRef"/> for a <see cref="IDependentItem"/> that can be optional or not.
        /// </summary>
        /// <param name="this">This <see cref="IDependentItem"/>. Can be null.</param>
        /// <param name="optional">False to obtain a non optional reference (same as calling <see cref="GetReference(IDependentItem)"/>).</param>
        /// <returns>A reference to the item that is optional by default. Null if <paramref name="this"/> is null.</returns>
        public static IDependentItemRef GetOptionalReference( this IDependentItem @this, bool optional = true )
        {
            if( !optional ) return GetReference( @this );
            return @this != null ? new OptItemRef( @this ) : null;
        }

        /// <summary>
        /// Obtains an <see cref="IDependentItemGroupRef"/> for a <see cref="IDependentItemGroup"/> that can be optional or not.
        /// </summary>
        /// <param name="this">This <see cref="IDependentItemGroup"/>. Can be null.</param>
        /// <param name="optional">False to obtain a non optional reference (same as calling <see cref="GetReference(IDependentItemGroup)"/>).</param>
        /// <returns>A reference to the group that is optional by default. Null if <paramref name="this"/> is null.</returns>
        public static IDependentItemGroupRef GetOptionalReference( this IDependentItemGroup @this, bool optional = true )
        {
            if( !optional ) return GetReference( @this );
            return @this != null ? new OptItemRef( @this ) : null;
        }

        /// <summary>
        /// Obtains an <see cref="IDependentItemContainerRef"/> for a <see cref="IDependentItemContainer"/> that can be optional or not.
        /// </summary>
        /// <param name="this">This <see cref="IDependentItemContainer"/>. Can be null.</param>
        /// <param name="optional">False to obtain a non optional reference (same as calling <see cref="GetReference(IDependentItemContainer)"/>).</param>
        /// <returns>A reference to the container that is optional by default. Null if <paramref name="this"/> is null.</returns>
        public static IDependentItemContainerRef GetOptionalReference( this IDependentItemContainer @this, bool optional = true )
        {
            if( !optional ) return GetReference( @this );
            return @this != null ? new OptItemRef( @this ) : null;
        }
        
        #endregion

        #region GetReference and GetOptionalReference on IDependentItemRef, IDependentItemGroupRef and IDependentItemContainerRef

        /// <summary>
        /// Obtains a non optional reference to an item from a potentially optional referenced item.
        /// </summary>
        /// <param name="this">This <see cref="IDependentItemRef"/>. Can be null.</param>
        /// <returns>A non optional reference to the item.</returns>
        public static IDependentItemRef GetReference( this IDependentItemRef @this )
        {
            if( @this != null && @this.Optional )
            {
                DirectRef d = @this as DirectRef;
                if( d != null ) return GetReference( d.Item );
                return new NamedDependentItemRef( @this.FullName );
            }
            return @this;
        }

        /// <summary>
        /// Obtains a non optional reference to a group from a potentially optional referenced group.
        /// </summary>
        /// <param name="this">This <see cref="IDependentItemGroupRef"/>. Can be null.</param>
        /// <returns>A non optional reference to the referenced group.</returns>
        public static IDependentItemGroupRef GetReference( this IDependentItemGroupRef @this )
        {
            if( @this != null && @this.Optional )
            {
                DirectRef d = @this as DirectRef;
                if( d != null ) return GetReference( (IDependentItemGroup)d.Item );
                return new NamedDependentItemGroupRef( @this.FullName );
            }
            return @this;
        }

        /// <summary>
        /// Obtains a non optional reference to a container from a potentially optional referenced container.
        /// </summary>
        /// <param name="this">This <see cref="IDependentItemContainerRef"/>. Can be null.</param>
        /// <returns>A non optional reference to the referenced container.</returns>
        public static IDependentItemContainerRef GetReference( this IDependentItemContainerRef @this )
        {
            if( @this != null && @this.Optional )
            {
                DirectRef d = @this as DirectRef;
                if( d != null ) return GetReference( (IDependentItemContainer)d.Item );
                return new NamedDependentItemContainerRef( @this.FullName );
            }
            return @this;
        }

        /// <summary>
        /// Obtains an optional reference to an item from a referenced item. If <paramref name="this"/> is already optional (or null), it is returned as-is.
        /// </summary>
        /// <param name="this">This <see cref="IDependentItemRef"/>. Can be null.</param>
        /// <param name="optional">False to obtain a non optional reference (same as calling <see cref="GetReference(IDependentItemRef)"/>).</param>
        /// <returns>A reference to the item that is optional by default. Null if <paramref name="this"/> is null.</returns>
        public static IDependentItemRef GetOptionalReference( this IDependentItemRef @this, bool optional = true )
        {
            if( !optional ) return GetReference( @this );
            if( @this == null || @this.Optional ) return @this;
            DirectRef d = @this as DirectRef;
            if( d != null ) return new OptItemRef( d.Item );
            return new NamedDependentItemRef( @this.FullName, true );
        }

        /// <summary>
        /// Obtains an optional reference to a group from a referenced group. If <paramref name="this"/> is already optional (or null), it is returned as-is.
        /// </summary>
        /// <param name="this">This <see cref="IDependentItemGroupRef"/>. Can be null.</param>
        /// <param name="optional">False to obtain a non optional reference (same as calling <see cref="GetReference(IDependentItemGroupRef)"/>).</param>
        /// <returns>A reference to the item that is optional by default. Null if <paramref name="this"/> is null.</returns>
        public static IDependentItemGroupRef GetOptionalReference( this IDependentItemGroupRef @this, bool optional = true )
        {
            if( !optional ) return GetReference( @this );
            if( @this == null || @this.Optional ) return @this;
            DirectRef d = @this as DirectRef;
            if( d != null ) return new OptItemRef( d.Item );
            return new NamedDependentItemGroupRef( @this.FullName, true );
        }

        /// <summary>
        /// Obtains an optional reference to a container from a referenced container. If <paramref name="this"/> is already optional (or null), it is returned as-is.
        /// </summary>
        /// <param name="this">This <see cref="IDependentItemContainerRef"/>. Can be null.</param>
        /// <param name="optional">False to obtain a non optional reference (same as calling <see cref="GetReference(IDependentItemContainerRef)"/>).</param>
        /// <returns>A reference to the item that is optional by default. Null if <paramref name="this"/> is null.</returns>
        public static IDependentItemContainerRef GetOptionalReference( this IDependentItemContainerRef @this, bool optional = true )
        {
            if( !optional ) return GetReference( @this );
            if( @this == null || @this.Optional ) return @this;
            DirectRef d = @this as DirectRef;
            if( d != null ) return new OptItemRef( d.Item );
            return new NamedDependentItemContainerRef( @this.FullName, true );
        }

        #endregion


    }
}
