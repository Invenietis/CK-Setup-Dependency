using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setup
{
    /// <summary>
    /// Static entry points to topological sort algorithm.
    /// </summary>
    public static class DependencySorter
    {
        static readonly DependencySorterOptions _defaultOptions = new DependencySorterOptions();

        /// <summary>
        /// Try to order items. First cycle encountered is detected, missing dependencies are 
        /// collected and resulting ordered items are initialized in the correct order.
        /// </summary>
        /// <param name="monitor">
        /// Monitor that enables <see cref="IDependentItem.StartDependencySort(IActivityMonitor)"/>
        /// to signal error or fatal.
        /// </param>
        /// <param name="items">Set of <see cref="IDependentItem"/> to order.</param>
        /// <param name="discoverers">An optional set of <see cref="IDependentItemDiscoverer"/> (can be null).</param>
        /// <param name="options">Options for advanced uses.</param>
        /// <returns>A <see cref="IDependencySorterResult"/>.</returns>
        public static IDependencySorterResult OrderItems( IActivityMonitor monitor, IEnumerable<IDependentItem> items, IEnumerable<IDependentItemDiscoverer> discoverers, DependencySorterOptions options = null )
        {
            return DependencySorter<IDependentItem>.OrderItems( monitor, items, discoverers, options );
        }

        /// <summary>
        /// Try to order items. First cycle encountered is detected, missing dependencies are 
        /// collected and resulting ordered items are initialized in the correct order.
        /// </summary>
        /// <param name="monitor">
        /// Monitor that enables <see cref="IDependentItem.StartDependencySort(IActivityMonitor)"/>
        /// to signal error or fatal.
        /// </param>
        /// <param name="items">Set of <see cref="IDependentItem"/> to order.</param>
        /// <returns>A <see cref="IDependencySorterResult"/>.</returns>
        public static IDependencySorterResult OrderItems( IActivityMonitor monitor,  params IDependentItem[] items )
        {
            return DependencySorter<IDependentItem>.OrderItems( monitor, items, null, null );
        }

        /// <summary>
        /// Try to order items. First cycle encountered is detected, missing dependencies are 
        /// collected and resulting ordered items are initialized in the correct order.
        /// </summary>
        /// <param name="monitor">
        /// Monitor that enables <see cref="IDependentItem.StartDependencySort(IActivityMonitor)"/>
        /// to signal error or fatal.
        /// </param>
        /// <param name="reverseName">True to reverse lexicographic order for items that share the same rank.</param>
        /// <param name="items">Set of <see cref="IDependentItem"/> to order.</param>
        /// <returns>A <see cref="IDependencySorterResult"/>.</returns>
        public static IDependencySorterResult OrderItems( IActivityMonitor monitor, bool reverseName, params IDependentItem[] items )
        {
            return DependencySorter<IDependentItem>.OrderItems( monitor, items, null, new DependencySorterOptions() { ReverseName = reverseName } );
        }

        /// <summary>
        /// Try to order items. First cycle encountered is detected, missing dependencies are 
        /// collected and resulting ordered items are initialized in the correct order.
        /// </summary>
        /// <param name="monitor">
        /// Monitor that enables <see cref="IDependentItem.StartDependencySort(IActivityMonitor)"/>
        /// to signal error or fatal.
        /// </param>
        /// <param name="options">Options for advanced uses.</param>
        /// <param name="items">Set of <see cref="IDependentItem"/> to order.</param>
        /// <returns>A <see cref="IDependencySorterResult"/>.</returns>
        public static IDependencySorterResult OrderItems( IActivityMonitor monitor,  DependencySorterOptions options, params IDependentItem[] items )
        {
            return DependencySorter<IDependentItem>.OrderItems( monitor, items, null, options );
        }
    
    }
}
