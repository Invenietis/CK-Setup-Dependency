#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setup.Dependency\ISortedItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;

namespace CK.Setup
{
    /// <summary>
    /// Generic version of <see cref="ISortedItem"/> where <typeparam name="T">type parameter</typeparam> is a <see cref="IDependentItem"/>.
    /// A sorted item can be directly associated to a IDependentItem, a <see cref="IDependentItemContainer"/> or can be the head for a container.
    /// </summary>
    public interface ISortedItem<T> : ISortedItem where T : IDependentItem
    {
        /// <summary>
        /// Gets the associated item.
        /// </summary>
        new T Item { get; }

        /// <summary>
        /// Gets the container to which this item belongs thanks to its own configuration (<see cref="IDependentItem.Container"/>.
        /// If the actual <see cref="Container"/> is inherited through <see cref="Generalization"/>, this ConfiguredContainer is null.
        /// </summary>
        new ISortedItem<T> ConfiguredContainer { get; }

        /// <summary>
        /// Gets the container to which this item belongs.
        /// Use <see cref="HeadForGroup"/> to get its head.
        /// </summary>
        new ISortedItem<T> Container { get; }

        /// <summary>
        /// Gets the Generalization of this item if it has one.
        /// </summary>
        new ISortedItem<T> Generalization { get; }

        /// <summary>
        /// Gets the head of the group if this item is a group (null otherwise).
        /// </summary>
        new ISortedItem<T> HeadForGroup { get; }

        /// <summary>
        /// Gets the group for which this item is the Head. 
        /// Null if this item is not a Head.
        /// </summary>
        new ISortedItem<T> GroupForHead { get; }

        /// <summary>
        /// Gets a clean set of requirements for the item. Combines direct <see cref="IDependentItem.Requires"/>
        /// and <see cref="IDependentItem.RequiredBy"/> declared by existing other items without any duplicates.
        /// Defaults to an empty enumerable.
        /// Requirement to the <see cref="IDependentItem.Generalization"/> is always removed.
        /// Requirements to any Container are removed when <see cref="DependencySorterOptions.SkipDependencyToContainer"/> is true.
        /// </summary>
        new IEnumerable<ISortedItem<T>> Requires { get; }

        /// <summary>
        /// Gets the direct requirements for the item (it the direct mapping of <see cref="IDependentItem.Requires"/>)
        /// to their associated to sorted items.
        /// Defaults to an empty enumerable.
        /// </summary>
        new IEnumerable<ISortedItem<T>> DirectRequires { get; }

        /// <summary>
        /// Gets the groups (as their <see cref="ISortedItem{T}"/> wrapper) to which this item belongs.
        /// Defaults to an empty enumerable.
        /// </summary>
        new IEnumerable<ISortedItem<T>> Groups { get; }

        /// <summary>
        /// Gets the items (as their <see cref="ISortedItem{T}"/> wrapper) that are contained in 
        /// the <see cref="Item"/> if it is a <see cref="IDependentItemGroup"/> (that can be a <see cref="IDependentItemContainer"/>).
        /// Empty otherwise.
        /// </summary>
        new IEnumerable<ISortedItem<T>> Children { get; }

        /// <summary>
        /// Gets all the items recursively (as their <see cref="ISortedItem{T}"/> wrapper) that are contained in 
        /// the <see cref="Item"/> if it is a <see cref="IDependentItemGroup"/> (that can be a <see cref="IDependentItemContainer"/>).
        /// Groups introduce a complexity here (a group contains items that belong to a container or other groups): this enumeration 
        /// removes duplicates and corretcly handles any cycles that may exist.
        /// Empty otherwise.
        /// </summary>
        new IEnumerable<ISortedItem<T>> AllChildren { get; }

    }
}
