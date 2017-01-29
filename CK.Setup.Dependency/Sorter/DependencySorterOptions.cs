using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setup
{
    /// <summary>
    /// Parametrizes the way <see cref="G:DependencySorter.OrderItems"/> works.
    /// </summary>
    public class DependencySorterOptions
    {
        /// <summary>
        /// Gets or sets whether to reverse the lexicographic order for items that share the same rank.
        /// Defaults to false.
        /// </summary>
        public bool ReverseName { get; set; }

        /// <summary>
        /// Gets or sets whether dependencies to any Container the item belongs to should be ignored.
        /// Defaults to false.
        /// </summary>
        public bool SkipDependencyToContainer { get; set; }

        /// <summary>
        /// Gets or sets a function that will be called with the list of items once all of them are registered.
        /// Duplicates has been removed.
        /// </summary>
        public Action<IEnumerable<IDependentItem>> HookInput { get; set; }

        /// <summary>
        /// Gets or sets a function that will be called when items have been sorted.
        /// The final <see cref="IDependencySorterResult"/> may not be successful (ie. <see cref="IDependencySorterResult.HasStructureError"/> may be true),
        /// but if a cycle has been detected, this hook is not called.
        /// </summary>
        public Action<IEnumerable<ISortedItem>> HookOutput { get; set; }
    }

}
