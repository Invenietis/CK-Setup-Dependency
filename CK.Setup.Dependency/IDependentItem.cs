#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setup.Dependency\IDependentItem.cs) is part of CK-Database. 
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
    /// An item that is identified by a <see cref="IDependentItemRef.FullName">FullName</see>, can be in 
    /// a <see cref="IDependentItemContainer"/> and have dependencies onto other items. 
    /// The <see cref="DependencySorter"/> is used to order such items based on their dependencies.
    /// </summary>
    public interface IDependentItem
    {
        /// <summary>
        /// Gets a name that uniquely identifies the item. 
        /// It must be not null nor empty and should not be longer than 400 characters.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Gets a reference to the container to which this item belongs. 
        /// Null if this item does not belong to a container.
        /// </summary>
        /// <remarks>
        /// If a container exists, this item may belong to <see cref="IDependentItemGroup.Children">Children</see>
        /// (but it is not mandatory as long as the <see cref="DependencySorter"/> is concerned: it will
        /// correctly handle all cases).
        /// </remarks>
        IDependentItemContainerRef Container { get; }

        /// <summary>
        /// Gets a reference to the item that generalizes this one. 
        /// Null if this item does not specialize any other item.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This "Generalization" relationships is a requirement (like any other in <see cref="Requires"/>, it 
        /// can be <see cref="IDependentItemRef.Optional"/>), with an addition: a Generalization's <see cref="Container"/> is considered as the 
        /// Container for any of its specializations that do not have one.
        /// </para>
        /// <para>
        /// This relationships enables a kind of "Container inheritance": the <see cref="Container">IDependentItem.Container</see> is 
        /// considered as an item's attribute that is inherited by its specialized items.
        /// </para>
        /// </remarks>
        IDependentItemRef Generalization { get; }

        /// <summary>
        /// Gets this item's dependencies. Can be null if no such dependency exists.
        /// </summary>
        /// <remarks>
        /// When <see cref="IDependentItemRef.Optional"/> is true, if it is not found, this
        /// will not be an error (see <see cref="DependentItemIssue.MissingDependencies"/>).
        /// </remarks>
        IEnumerable<IDependentItemRef> Requires { get; }

        /// <summary>
        /// Gets the revert dependencies (an item can specify that it is itself required by another one). 
        /// A "RequiredBy" constraint is optional: a missing "RequiredBy" is not an error (it is considered 
        /// as a reverted optional dependency).
        /// Can be null if no such dependency exists.
        /// </summary>
        IEnumerable<IDependentItemRef> RequiredBy { get; }

        /// <summary>
        /// Gets the groups to which this item belongs. If one of these groups is a container, it must be 
        /// the only container of this item (otherwise it is an error).
        /// Can be null if the item is not in any group.
        /// </summary>
        IEnumerable<IDependentItemGroupRef> Groups { get; }

        /// <summary>
        /// Allows the dependent item to prepare itself before ordering. The returned object (if any)
        /// is made available after the sort in <see cref="ISortedItem.StartValue"/>.
        /// </summary>
        /// <param name="m">Monitor that can be use to signal error. Any error or fatal logged wil stop the process.</param>
        /// <returns>Any object that has to be associated to this item and a <see cref="G:DependencySorter.OrderItems"/> call.</returns>
        object StartDependencySort( IActivityMonitor m );
    }
}

