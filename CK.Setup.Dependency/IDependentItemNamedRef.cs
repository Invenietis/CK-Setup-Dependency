#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setup.Dependency\IDependentItemNamedRef.cs) is part of CK-Database. 
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
    /// Defines a named reference to an item: this interface must be supported by items that really are named references
    /// to items (such as <see cref="NamedDependentItemRef"/>). 
    /// As it is a true reference (name is not defined by the object itself), this offer a way to manipulate the referenced <see cref="IDependentItem.FullName"/>.
    /// </summary>
    public interface IDependentItemNamedRef : IDependentItemRef 
    {
        /// <summary>
        /// Sets the full name of this reference. It MUST return the modified instance (be it this).
        /// </summary>
        /// <param name="fullName">New full name. Must not be null.</param>
        /// <returns>This instance or a new one if required (this is let to the implementation, but see remarks).</returns>
        /// <remarks>
        /// Even if nothing prevents this reference to be altered by this call, we prefer the "immutable objects" way here: concrete 
        /// implementations such as <see cref="NamedDependentItemRef"/> are immutable and create a new instance if needed.
        /// </remarks>
        IDependentItemNamedRef SetFullName( string fullName );
    
    }
}
