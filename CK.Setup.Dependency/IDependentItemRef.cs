#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setup.Dependency\IDependentItemRef.cs) is part of CK-Database. 
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
    /// Defines a reference to an item. When a <see cref="IDependentItem"/> implements
    /// its own IDependentItemRef and when the object item is known it can be used 
    /// as a (non optional) reference. The struct <see cref="NamedDependentItemRef"/> must be used for 
    /// optional or pure named reference.
    /// </summary>
    public interface IDependentItemRef
    {
        /// <summary>
        /// Gets a name that uniquely identifies the item. It must be not null nor starts with '?'.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Gets whether the reference is an optional one.
        /// </summary>
        bool Optional { get; }

    }
}
