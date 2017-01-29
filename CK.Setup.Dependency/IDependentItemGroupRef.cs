#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setup.Dependency\IDependentItemGroupRef.cs) is part of CK-Database. 
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
    /// Defines a reference to a group. 
    /// </summary>
    /// <remarks>
    /// A <see cref="IDependentItemGroup"/> implementation should implement this 
    /// (it is then its own IDependentItemGroupRef): when the group object exists it can be used
    /// as a (non optional) reference. The struct <see cref="NamedDependentItemGroupRef"/> must be used for 
    /// optional or pure named reference.
    /// </remarks>
    public interface IDependentItemGroupRef : IDependentItemRef
    {
    }
}
