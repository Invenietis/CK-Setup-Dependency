#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setup.Dependency\IDependentItemGroup.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Collections.Generic;

namespace CK.Setup;

/// <summary>
/// Collection part of the composite <see cref="IDependentItem"/>. 
/// It only has to expose its <see cref="Children"/>.
/// </summary>
public interface IDependentItemGroup : IDependentItem
{
    /// <summary>
    /// Gets a list of children. Can be null or empty.
    /// </summary>
    /// <remarks>
    /// The <see cref="DependencySorter"/> uses this list to discover the original <see cref="IDependentItem"/> to order.
    /// </remarks>
    IEnumerable<IDependentItemRef>? Children { get; }

}
