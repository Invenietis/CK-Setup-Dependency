#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setup.Dependency\IDependentItemContainer.cs) is part of CK-Database. 
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
    /// Specializes <see cref="IDependentItemGroup"/> that defines an object as a container: any <see cref="IDependentItem.Container"/>
    /// can reference it.
    /// </summary>
    /// <remarks>
    /// Provided that other items are submitted to the <see cref="G:DependencySorter.OrderItems"/> methods the <see cref="IDependentItemGroup.Children"/> collection 
    /// can be null or empty since any submitted items that has its <see cref="IDependentItem.Container"/> references this container will be automatically "added"
    /// to the container.
    /// </remarks>
    public interface IDependentItemContainer : IDependentItemGroup
    {
    }
}
