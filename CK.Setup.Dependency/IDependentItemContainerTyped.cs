#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setup.Dependency\IDependentItemContainerTyped.cs) is part of CK-Database. 
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
    /// This interface enables an object that is a <see cref="IDependentItemContainer"/> to 
    /// dynamically choose to be considered as a simple <see cref="IDependentItem"/> 
    /// or a <see cref="IDependentItemGroup"/> only. 
    /// This is for advanced use, see remarks.
    /// </summary>
    /// <remarks>
    /// This is a way to enforce (more or less) dynamic rules in a dependency graph.
    /// This is also for optimization: a simple item does not generate a head. If the items to be sorted
    /// are known and no named references are used, this enables a potential container to be considered as 
    /// a simple item if it assumes that it has no content.
    /// </remarks>
    public interface IDependentItemContainerTyped : IDependentItemContainer
    {
        /// <summary>
        /// Gets the kind of this item (can be <see cref="DependentItemKind.Item"/>, <see cref="DependentItemKind.Group"/> 
        /// or <see cref="DependentItemKind.Container"/>).
        /// </summary>
        DependentItemKind ItemKind { get; }
    }
}
