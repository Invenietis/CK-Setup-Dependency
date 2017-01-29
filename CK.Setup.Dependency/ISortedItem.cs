using System;
using System.Collections.Generic;

namespace CK.Setup
{
    /// <summary>
    /// A sorted item can be directly associated to a <see cref="IDependentItem"/>, a <see cref="IDependentItemContainer"/> 
    /// or can be the head for a container.
    /// </summary>
    public interface ISortedItem
    {
        /// <summary>
        /// Gets the associated item.
        /// </summary>
        IDependentItem Item { get; }

        /// <summary>
        /// Gets the item type.
        /// </summary>
        DependentItemKind ItemKind { get; }

        /// <summary>
        /// Gets the index of this item among the others.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Gets the rank of this item.
        /// </summary>
        int Rank { get; }

        /// <summary>
        /// Gets the full name of the item.
        /// It is suffixed by ".Head" if this is a head.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Gets the object returned by <see cref="IDependentItem.StartDependencySort"/> if any.
        /// </summary>
        object StartValue { get; }

        /// <summary>
        /// Gets the container to which this item belongs thanks to its own configuration (<see cref="IDependentItem.Container"/>.
        /// If the actual <see cref="Container"/> is inherited through <see cref="Generalization"/>, this ConfiguredContainer is null.
        /// </summary>
        ISortedItem ConfiguredContainer { get; }

        /// <summary>
        /// Gets the container to which this item belongs.
        /// Use <see cref="HeadForGroup"/> to get its head.
        /// </summary>
        ISortedItem Container { get; }

        /// <summary>
        /// Gets the Generalization of this item if it has one.
        /// </summary>
        ISortedItem Generalization { get; }

        /// <summary>
        /// Whether this is the head of a group.
        /// Use <see cref="GroupForHead"/> to get the associated group.
        /// </summary>
        bool IsGroupHead { get; }

        /// <summary>
        /// Whether this is a group (it is a Container if <see cref="ItemKind"/> is <see cref="DependentItemKind.Container"/>.
        /// Use <see cref="HeadForGroup"/> to get the associated head.
        /// </summary>
        bool IsGroup { get; }

        /// <summary>
        /// Gets the head of the group if this item is a group (null otherwise).
        /// </summary>
        ISortedItem HeadForGroup { get; }

        /// <summary>
        /// Gets the group for which this item is the Head. 
        /// Null if this item is not a Head.
        /// </summary>
        ISortedItem GroupForHead { get; }

        /// <summary>
        /// Gets a clean set of requirements for the item. Combines direct <see cref="IDependentItem.Requires"/>
        /// and <see cref="IDependentItem.RequiredBy"/> declared by existing other items without any duplicates.
        /// Defaults to an empty enumerable.
        /// Requirement to the <see cref="IDependentItem.Generalization"/> is always removed.
        /// Requirements to any Container are removed when <see cref="DependencySorterOptions.SkipDependencyToContainer"/> is true.
        /// </summary>
        IEnumerable<ISortedItem> Requires { get; }

        /// <summary>
        /// Gets the direct requirements for the item (it the direct mapping of <see cref="IDependentItem.Requires"/>)
        /// to their associated to sorted items.
        /// Defaults to an empty enumerable.
        /// </summary>
        IEnumerable<ISortedItem> DirectRequires { get; }

        /// <summary>
        /// Gets the groups (as their <see cref="ISortedItem"/> wrapper) to which this item belongs.
        /// Defaults to an empty enumerable.
        /// </summary>
        IEnumerable<ISortedItem> Groups { get; }

        /// <summary>
        /// Gets the items (as their <see cref="ISortedItem"/> wrapper) that are contained in 
        /// the <see cref="Item"/> if it is a <see cref="IDependentItemGroup"/> (that can be a <see cref="IDependentItemContainer"/>).
        /// Empty otherwise.
        /// </summary>
        IEnumerable<ISortedItem> Children { get; }

        /// <summary>
        /// Gets all the items recursively (as their <see cref="ISortedItem"/> wrapper) that are contained in 
        /// the <see cref="Item"/> if it is a <see cref="IDependentItemGroup"/> (that can be a <see cref="IDependentItemContainer"/>).
        /// Groups introduce the a complexity here (a group contains items that belong to a container or other groups): this enumeration 
        /// removes duplicates and corretcly handles any cycles that may exist.
        /// Empty otherwise.
        /// </summary>
        IEnumerable<ISortedItem> AllChildren { get; }
    }

}
