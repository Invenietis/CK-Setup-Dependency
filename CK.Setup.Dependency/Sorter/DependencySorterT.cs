using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{

    /// <summary>
    /// Static class that offers <see cref="IDependentItem"/> ordering functionnality thanks to <see cref="G:OrderItems"/> methods;
    /// </summary>
    public static class DependencySorter<T> where T : class, IDependentItem
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
        public static DependencySorterResult<T> OrderItems( IActivityMonitor monitor, IEnumerable<T> items, IEnumerable<IDependentItemDiscoverer<T>> discoverers, DependencySorterOptions options = null )
        {
            var computer = new RankComputer( monitor, items, discoverers, options ?? _defaultOptions );
            computer.Process();
            return computer.GetResult();
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
        /// <returns>A <see cref="DependencySorterResult{T}"/>.</returns>
        public static DependencySorterResult<T> OrderItems( IActivityMonitor monitor,  params T[] items )
        {
            return OrderItems( monitor, items, null, null );
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
        /// <returns>A <see cref="DependencySorterResult{T}"/>.</returns>
        public static DependencySorterResult<T> OrderItems( IActivityMonitor monitor, bool reverseName, params T[] items )
        {
            return OrderItems( monitor, items, null, new DependencySorterOptions() { ReverseName = reverseName } );
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
        /// <returns>A <see cref="DependencySorterResult{T}"/>.</returns>
        public static DependencySorterResult<T> OrderItems( IActivityMonitor monitor, DependencySorterOptions options, params T[] items )
        {
            return OrderItems( monitor, items, null, options );
        }

        internal class Entry : ISortedItem<T>
        {
            // This marker saves one iteration over specialized items to resolve Generalizations.
            internal static readonly Entry GeneralizationMissingMarker = new Entry( null, String.Empty );

            // Reference to the entries of the 
            // This is unfortunately required only for ISortedItem.Requires to give ISortedItems instead of
            // poor IDependentItemRef. Mapping from a reference (FullName) to its Entry is done dynamically 
            // by a LINQ Select. The other way would be to compute the list of Entries for each ISortedItem.Requires
            // before returning the result (this would anyway imply a dedicated field to store the result of the mapping).
            // Since ISortedItem.Requires is not necessarily called, I choose the dynamic way... It's a pity but there
            // is no way to it differently...
            readonly Dictionary<object, object> _entries;

            public Entry( Dictionary<object, object> entries, string fullName )
            {
                _entries = entries;
                FullName = fullName;
                Rank = -1;
            }
            
            Entry( Entry group )
            {
                _entries = group._entries;
                Item = group.Item;
                ItemKind = group.ItemKind;
                FullName = group.FullName + ".Head";
                Rank = -1;
                GroupIfHead = group;
            }
            

            public bool Init( T e, DependentItemKind actualType, object startValue )
            {
                Debug.Assert( FullName == e.FullName );
                Item = e;
                StartValue = startValue;
                ItemKind = actualType;
                if( actualType != DependentItemKind.Item )
                {
                    Debug.Assert( HeadIfGroupOrContainer == null, "Only once!" );
                    Debug.Assert( FirstChildIfContainer == null );
                    HeadIfGroupOrContainer = new Entry( this );
                    if( actualType == DependentItemKind.Group ) GroupChildren = new List<Entry>();
                    return true;
                }
                return false;
            }
            
            /// <summary>
            /// FullName of the Item (or Item.FullName + ".Head" for heads).
            /// </summary>
            public readonly string FullName;

            /// <summary>
            /// Item is set for all kind of entries: Containers, Heads and normal item all reference the dependent item.
            /// </summary>
            public T Item { get; private set; }

            /// <summary>
            /// Updated from actual IDependentItem type and IDependentItemContainerTyped.
            /// For heads, it is the same as its GroupIfHead.ItemType (ie. the type of the Item is copied).
            /// </summary>
            public DependentItemKind ItemKind { get; private set; }

            /// <summary>
            /// Captured return from Item.StartDependencySort() call.
            /// </summary>
            public object StartValue { get; private set; }
            
            /// <summary>
            /// Allocated as soon as this entry Requires another one.
            /// </summary>
            public HashSet<IDependentItemRef> Requires;
            
            /// <summary>
            /// Generalization entry if it exists.
            /// </summary>
            public Entry Generalization;

            /// <summary>
            /// Container entry if it exists.
            /// </summary>
            public Entry Container { get; private set; }

            /// <summary>
            /// The GroupIfHead is null for normal items, groups and containers.
            /// It is not null only for heads.
            /// </summary>
            public Entry GroupIfHead { get; private set; }

            /// <summary>
            /// The HeadIfGroupOrContainer is null for normal items and heads.
            /// It is not null only for container or group.
            /// </summary>
            public Entry HeadIfGroupOrContainer { get; private set; }

            /// <summary>
            /// Not null only when ItemType == Group.
            /// Container's children are managed by the FirstChildIfContainer / NextChildInContainer linked list.
            /// </summary>
            public List<Entry> GroupChildren { get; private set; }
            /// <summary>
            /// Allocated as soon as the item is added to a Group.
            /// Always null for heads.
            /// </summary>
            public HashSet<Entry> Groups { get; private set; }

            public Entry FirstChildIfContainer { get; private set; }
            public Entry NextChildInContainer { get; private set; }

            /// <summary>
            /// Computed by RankComputer.Process(). 
            /// </summary>
            public int Rank;

            /// <summary>
            /// Index is computed at the end of the process. 
            /// </summary>
            public int Index;

            internal void AddRequiredByRequires( IDependentItemRef req )
            {
                if( Requires == null ) Requires = new HashSet<IDependentItemRef>();
                Requires.Add( req );
            }

            internal void AddToGroup( Entry child )
            {
                Debug.Assert( ItemKind == DependentItemKind.Group );
                Debug.Assert( child != null );
                Debug.Assert( child.GroupIfHead == null, "Never add a head as a Child." );
                Debug.Assert( GroupChildren != null );               
                if( child.Groups == null ) child.Groups = new HashSet<Entry>();
                if( child.Groups.Add( this ) ) GroupChildren.Add( child );
            }

            internal void AddToContainer( Entry child )
            {
                Debug.Assert( ItemKind == DependentItemKind.Container );
                Debug.Assert( child.Container == null, "One and only one Container setting." );
                Debug.Assert( child != null );
                Debug.Assert( child.GroupIfHead == null, "Never add a head as a Child." );
                CheckContainerNotContains( child );
                child.NextChildInContainer = FirstChildIfContainer;
                FirstChildIfContainer = child;
                child.Container = this;
                if( child.HeadIfGroupOrContainer != null ) child.HeadIfGroupOrContainer.Container = this;
            }

            internal bool AppearInContainerChain( Entry dep )
            {
                Debug.Assert( dep.ItemKind == DependentItemKind.Container, "Called only with a Container." );
                Entry c = Container;
                while( c != null )
                {
                    if( c == dep ) return true;
                    c = c.Container;
                }
                return false;
            }

            [Conditional( "DEBUG" )]
            internal void CheckContainerIsHeadOrContains( Entry e )
            {
                if( e.GroupIfHead != null ) 
                {
                    Debug.Assert( e.GroupIfHead.Container == this, "e is a container's head ==> this is the container..." );
                    return;
                }
                Debug.Assert( HeadIfGroupOrContainer != null, "This is a Group..." );
                Debug.Assert( ItemKind == DependentItemKind.Container, "...more than that: a Container." );
                Entry c = FirstChildIfContainer;
                while( c != null )
                {
                    if( c == e ) return;
                    c = c.NextChildInContainer;
                }
                Debug.Fail( String.Format( "Group {0} does not contain item {1}.", FullName, e.FullName ) );
            }

            [Conditional( "DEBUG" )]
            internal void CheckContainerNotContains( Entry e )
            {
                Debug.Assert( HeadIfGroupOrContainer != null, "This is a Group..." );
                Debug.Assert( ItemKind == DependentItemKind.Container, "...more than that: a Container." );
                Entry c = FirstChildIfContainer;
                while( c != null )
                {
                    if( c == e ) Debug.Fail( String.Format( "Group {0} contains item {1}.", FullName, e.FullName ) ); ;
                    c = c.NextChildInContainer;
                }
            }

            public override string ToString()
            {
                return FullName;
            }

            #region ISortedItem

            int ISortedItem.Index => Index; 

            string ISortedItem.FullName => FullName; 

            int ISortedItem.Rank => Rank; 

            object ISortedItem.StartValue => StartValue; 

            bool ISortedItem.IsGroupHead => GroupIfHead != null; 

            bool ISortedItem.IsGroup => HeadIfGroupOrContainer != null; 

            ISortedItem ISortedItem.GroupForHead => GroupIfHead; 

            IDependentItem ISortedItem.Item => Item; 

            ISortedItem ISortedItem.Container => Container; 

            ISortedItem ISortedItem.ConfiguredContainer => ConfiguredContainer; 

            ISortedItem ISortedItem.HeadForGroup => HeadIfGroupOrContainer; 

            ISortedItem ISortedItem.Generalization => Generalization == GeneralizationMissingMarker ? null : Generalization;

            IEnumerable<ISortedItem> ISortedItem.Groups => GetGroups(); 

            IEnumerable<ISortedItem> ISortedItem.Requires => GetRequires(); 

            IEnumerable<ISortedItem> ISortedItem.DirectRequires => GetDirectRequires(); 

            IEnumerable<ISortedItem> ISortedItem.Children => GetChildren(); 

            IEnumerable<ISortedItem> ISortedItem.AllChildren => GetAllChildren( new HashSet<Entry>() ); 

            #endregion

            #region ISortedItem<T> Members

            ISortedItem<T> ISortedItem<T>.Container => Container; 

            // By double checking the full name, we handle any "Optional"ity of the original Container reference.
            public ISortedItem<T> ConfiguredContainer 
            {
                get { return Item.Container != null && ReferenceEquals( Item.Container.FullName, Container.FullName ) 
                                ? Container 
                                : null; } 
            }

            ISortedItem<T> ISortedItem<T>.Generalization =>  Generalization == GeneralizationMissingMarker 
                                                                ? null 
                                                                : Generalization; 

            ISortedItem<T> ISortedItem<T>.HeadForGroup => HeadIfGroupOrContainer; 

            ISortedItem<T> ISortedItem<T>.GroupForHead => GroupIfHead; 

            T ISortedItem<T>.Item => Item;

            IEnumerable<ISortedItem<T>> ISortedItem<T>.Requires => GetRequires();

            IEnumerable<ISortedItem<T>> ISortedItem<T>.DirectRequires => GetDirectRequires();

            IEnumerable<ISortedItem<T>> ISortedItem<T>.Groups => GetGroups(); 

            IEnumerable<ISortedItem<T>> ISortedItem<T>.Children => GetChildren(); 

            IEnumerable<ISortedItem<T>> ISortedItem<T>.AllChildren => GetAllChildren( new HashSet<Entry>() ); 

            #endregion
            IEnumerable<Entry> GetDirectRequires()
            {
                return RemoveMissing( Item.Requires );
            }


            IEnumerable<Entry> GetRequires()
            {
                return RemoveMissing( HeadIfGroupOrContainer != null ? HeadIfGroupOrContainer.Requires : Requires );
            }

            IEnumerable<Entry> RemoveMissing( IEnumerable<IDependentItemRef> r )
            {
                return r == null
                    ? Util.Array.Empty<Entry>()
                        // We can not blindly use (ISortedItem)_entries[r.FullName] because if DependencySorterResult.HasRequiredMissing is true
                        // and the resulting graph is nevertheless used (for Tracing by example) there will be no associated ISortedItem.
                        // ==> We must TryGetValue and filter unexisting sorted items.
                    : r.Select( r2 => (Entry)_entries.GetValueWithDefault( r2.FullName, null ) )
                        .Where( e => e != null );
            }

            IEnumerable<Entry> GetGroups()
            {
                // Groups is only on the Group (not on its head).
                var holder = GroupIfHead ?? this;
                return holder.Groups != null ? holder.Groups : (IEnumerable<Entry>)Util.Array.Empty<Entry>();
            }

            IEnumerable<Entry> GetChildren()
            {
                // GroupChildren is only on the Group (not on its head).
                var holder = GroupIfHead ?? this;
                return holder.GroupChildren != null ? holder.GroupChildren : holder.GetContainerChildren();
            }

            IEnumerable<Entry> GetContainerChildren()
            {
                var c = FirstChildIfContainer;
                while( c != null )
                {
                    yield return c;
                    c = c.NextChildInContainer;
                }
            }

            IEnumerable<Entry> GetAllChildren( HashSet<Entry> dedup )
            {
                foreach( var i in GetChildren() )
                {
                    if( dedup.Add( i ) )
                    {
                        yield return i;
                    }
                    foreach( var ii in i.GetAllChildren( dedup ) )
                    {
                        if( dedup.Add( ii ) )
                        {
                            yield return ii;
                        }
                    }
                }
            }


        }

        class RankComputer
        {
            readonly Dictionary<object, object> _entries;
            readonly List<Entry> _result;
            readonly List<DependentItemIssue> _itemIssues;
            readonly Comparison<Entry> _comparer;
            readonly DependencySorterOptions _options;
            readonly IActivityMonitor _monitor;
            List<CycleExplainedElement> _cycle;
            int _startErrorCount;
            bool _fatalError;

            public RankComputer( IActivityMonitor m, IEnumerable<T> items, IEnumerable<IDependentItemDiscoverer<T>> discoverers, DependencySorterOptions options )
            {
                _monitor = m;
                _entries = new Dictionary<object, object>();
                _result = new List<Entry>();
                _itemIssues = new List<DependentItemIssue>();
                _options = options;
                if( _options.ReverseName ) _comparer = ReverseComparer;
                else _comparer =  NormalComparer;

                Registerer r;
                using( m.OnError( () => _fatalError = true, () => ++_startErrorCount ) )
                {
                    r = new Registerer( this );
                    if( items != null ) r.Register( items );
                    if( discoverers != null )
                    {
                        foreach( var d in discoverers )
                        {
                            items = d.GetOtherItemsToRegister();
                            if( items != null ) r.Register( items );
                        }
                    }
                    r.FinalizeRegister();
                }
                Debug.Assert( _entries.Values.All( o => o is Entry ), "No more start values in dictionary once registered done." );
            }

            class Registerer
            {
                readonly Dictionary<object, object> _entries;
                readonly RankComputer _computer;
                readonly List<Entry> _namedContainersToBind;
                readonly List<Tuple<Entry, IDependentItemRef>> _childrenToBind;
                readonly List<Tuple<Entry, IDependentItemGroupRef>> _groupsToBind;
                readonly List<Entry> _specializedItems;

                public Registerer( RankComputer computer )
                {
                    _computer = computer;
                    _namedContainersToBind = new List<Entry>();
                    _childrenToBind = new List<Tuple<Entry, IDependentItemRef>>();
                    _groupsToBind = new List<Tuple<Entry, IDependentItemGroupRef>>();
                    _specializedItems = new List<Entry>();
                    _entries = _computer._entries;
                }

                public void Register( IEnumerable<T> items )
                {
                    foreach( T e in items )
                    {
                        RegisterEntry( e, null, null );
                    }
                }

                public void FinalizeRegister()
                {
                    foreach( Entry nc in _namedContainersToBind )
                    {
                        Debug.Assert( nc.Item.Container != null && !(nc.Item.Container is IDependentItemContainer) );
                        // Container has been set by the first Container that claims to own the item in its Children collection.
                        if( nc.Container != null )
                        {
                            // If it is the good one, it is perfect (we avoided a lookup in the dictionary :-)).
                            // Else... it is a multiple containment.
                            if( nc.Container.FullName != nc.Item.Container.FullName )
                            {
                                _computer.SetStructureError( nc, DependentItemStructureError.MultipleContainer ).AddExtraneousContainers( nc.Container.FullName );
                            }
                        }
                        else
                        {
                            // The entry has no associated container yet: we must find it by name.
                            Entry c;
                            if( TryGetEntryValue( nc.Item.Container.FullName, out c ) )
                            {
                                if( c.ItemKind == DependentItemKind.Container )
                                {
                                    // The named container exists and it is a Container.
                                    c.AddToContainer( nc );
                                }
                                else
                                {
                                    // The named item exists but is not a container.
                                    if( c.Item is IDependentItemContainer )
                                        _computer.SetStructureError( nc, DependentItemStructureError.ExistingContainerAskedToNotBeAContainer );
                                    else _computer.SetStructureError( nc, DependentItemStructureError.ExistingItemIsNotAContainer );
                                }
                            }
                            else if( !nc.Item.Container.Optional )
                            {
                                // The named container does not exist.
                                _computer.SetStructureError( nc, DependentItemStructureError.MissingNamedContainer );
                            }
                        }
                    }
                    foreach( var eC in _childrenToBind )
                    {
                        Entry group = eC.Item1;
                        Debug.Assert( group.HeadIfGroupOrContainer != null, "The entry is a Group." );
                        Entry child;
                        IDependentItemRef childRef = eC.Item2;
                        if( TryGetEntryValue( childRef.FullName, out child ) )
                        {
                            AddChildToGroupOrContainer( group, child );
                        }
                        else if( !childRef.Optional )
                        {
                            // The child has not been registered.
                            _computer.SetStructureError( group, DependentItemStructureError.MissingNamedChild ).AddMissingChild( childRef.FullName );
                        }
                    }
                    foreach( var eC in _groupsToBind )
                    {
                        Entry entry = eC.Item1;
                        IDependentItemGroupRef groupRef = eC.Item2;
                        Entry group;
                        if( TryGetEntryValue( groupRef.FullName, out group ) )
                        {
                            if( group.ItemKind == DependentItemKind.Item )
                            {
                                _computer.SetStructureError( entry, DependentItemStructureError.DeclaredGroupRefusedToBeAGroup ).AddInvalidGroup( groupRef.FullName );
                            }
                            else AddChildToGroupOrContainer( group, entry );
                        }
                        else if( !groupRef.Optional )
                        {
                            // The group has not been registered.
                            _computer.SetStructureError( entry, DependentItemStructureError.MissingNamedGroup ).AddMissingGroup( groupRef.FullName );
                        }
                    }

                    // Now that named containers and children have been handled, we can follow the Generalization chains
                    // to resolve all of them to their roots and apply Generalization's Container inheritance.
                    // The list contains only container and items, not head.
                    foreach( var sEntry in _specializedItems )
                    {
                        if( sEntry.Generalization == null ) ResolveGeneralization( sEntry );
                    }
                }

                private void AddChildToGroupOrContainer( Entry group, Entry child )
                {
                    Debug.Assert( group != null && group.ItemKind != DependentItemKind.Item );
                    Debug.Assert( child != null );
                    // The item declares no container (null), a valid container, or the item declares a name and the entry has been added to namedContainersToBind.
                    Debug.Assert( child.Item.Container == null || child.Item.Container is IDependentItemContainer || _namedContainersToBind.Contains( child ) );
                    
                    // Is it already bound to a Container?
                    if( child.Container != null )
                    {
                        if( group.ItemKind == DependentItemKind.Container )
                        {
                            if( child.Container != group )
                            {
                                // The child is bound to a container with another name: the late-bound name is an extraneous container.
                                _computer.SetStructureError( child, DependentItemStructureError.MultipleContainer ).AddExtraneousContainers( group.FullName );
                            }
                        }
                        else
                        {
                            group.AddToGroup( child );
                        }
                    }
                    else
                    {
                        // We set the container or add the child to the group.
                        if( group.ItemKind == DependentItemKind.Container )
                        {
                            group.AddToContainer( child );
                        }
                        else group.AddToGroup( child );
                    }
                }

                void ResolveGeneralization( Entry sEntry )
                {
                    var s = sEntry.Item;
                    var g = s.Generalization;
                    Debug.Assert( g != null && sEntry.Generalization == null, "The entry is a specialization that does not know its Generalization yet." );
                    // Loop guard & default Generalization if not found by name.
                    sEntry.Generalization = Entry.GeneralizationMissingMarker;
                    Entry gEntry;
                    if( TryGetEntryValue( g.FullName, out gEntry ) )
                    {
                        // Sets the Generalization object also on the head if 
                        // we are on a Container.
                        sEntry.Generalization = gEntry;
                        if( sEntry.HeadIfGroupOrContainer != null ) sEntry.HeadIfGroupOrContainer.Generalization = gEntry;

                        if( gEntry.Generalization == null && gEntry.Item.Generalization != null ) ResolveGeneralization( gEntry );
                        // The entry is bound to its Generalization. It is time to inherit Container.
                        if( sEntry.Container == null && gEntry.Container != null )
                        {
                            gEntry.Container.AddToContainer( sEntry );
                            // Checks (debug only).
                            Debug.Assert( sEntry.Container == gEntry.Container );
                            gEntry.Container.CheckContainerIsHeadOrContains( sEntry );
                        }
                    }
                    else if( !g.Optional )
                    {
                        // Not found... If it is optional, act as if there
                        // were no generalization.
                        _computer.SetStructureError( sEntry, DependentItemStructureError.MissingGeneralization );
                    }

                }

                /// <summary>
                /// Preregistering allows an object's <see cref="IDependentItem.StartDependencySort"/> to be called after 
                /// its direct dependencies or its container's StartDependencySort.
                /// </summary>
                object PreRegisterObjectDependencies( IDependentItem e, bool memorizeStartValue )
                {
                    object entryOrStartValue;
                    if( !_entries.TryGetValue( e, out entryOrStartValue ) )
                    {
                        // Marks the entry with a null start value to handle cycles.
                        // Dependency cycles are simply ignored at this stage: they will 
                        // be detected and handled during the Process phasis.
                        _entries.Add( e, null );
                        // Container is a direct dependency.
                        IDependentItem container = e.Container as IDependentItem;
                        if( container != null ) PreRegisterObjectDependencies( container, true );
                        // Generalization is a direct dependency.
                        IDependentItem gen = e.Generalization as IDependentItem;
                        if( gen != null ) PreRegisterObjectDependencies( gen, true );
                        // Pre register direct requirements.
                        var req = e.Requires;
                        if( req != null )
                        {
                            foreach( var d in req )
                            {
                                IDependentItem di = d as IDependentItem;
                                if( di != null ) PreRegisterObjectDependencies( di, true );
                            }
                        }
                        // Pre registers Groups.
                        var grp = e.Groups;
                        if( grp != null )
                        {
                            foreach( var g in grp )
                            {
                                IDependentItem gi = g as IDependentItem;
                                if( gi != null ) PreRegisterObjectDependencies( gi, true );
                            }
                        }
                        // Gives the item an opportunity to prepare its data (mainly its FullName).
                        entryOrStartValue = e.StartDependencySort( _computer._monitor );
                        if( memorizeStartValue && entryOrStartValue != null ) _entries[e] = entryOrStartValue;
                    }
                    return entryOrStartValue;
                }

                Entry RegisterEntry( IDependentItem eItem, Entry alreadyRegisteredGroup, Entry alreadyRegisteredChild )
                {
                    Debug.Assert( eItem != null );
                    if( !(eItem is T) )
                    {
                        throw new CKException( "Automatically discovered object '{2}' is of type '{0}' that does not implement '{1}'. When using DependencySorter<T>, all IDependentItem must be of type T.",
                            eItem.GetType().AssemblyQualifiedName, 
                            typeof( T ).Name,
                            eItem.FullName );
                    }
                    T e = (T)eItem;
                    Debug.Assert( alreadyRegisteredGroup == null || alreadyRegisteredChild == null, "Not coming from both sides at the same time." );
                    // Preregistering: collects Start values by calling StartDependencySort.
                    object startValue = PreRegisterObjectDependencies( e, false );
                    
                    Entry entry = startValue as Entry;
                    if( entry != null )
                    {
                        #region If the Entry exists, we only have to handle (group,container)/item relation and registered homonyms.
                        // If entry.Item != e, this is an homonym registered previously (code below): ignores it
                        // and returns the first named entry that has been registered (an Homonym StructureError has been pushed anyway).
                        if( entry.Item == e )
                        {
                            if( alreadyRegisteredGroup != null ) 
                            {
                                // We are coming from the registration of our Container or Group (code below).
                                AddChildToGroupOrContainer( alreadyRegisteredGroup, entry );
                            }
                        }
                        #endregion
                        return entry;
                    }

                    #region Compute actual item type (actualType, e, g and c)
                    IDependentItemGroup g = null;
                    IDependentItemContainer c = e as IDependentItemContainer;
                    DependentItemKind actualType = DependentItemKind.Item;
                    if( c != null )
                    {
                        g = c;
                        IDependentItemContainerTyped cTyped = e as IDependentItemContainerTyped;
                        actualType = cTyped != null ? cTyped.ItemKind : DependentItemKind.Container;
                    }
                    else
                    {
                        g = e as IDependentItemGroup;
                        if( g != null ) actualType = DependentItemKind.Group;
                    }
                    #endregion

                    if( TryGetEntryValue( e.FullName, out entry ) )
                    {
                        #region FullName exists (Homonym or RequiredBy registration).
                        // The setupable item name is known, but
                        // is there an item already associated to this name?
                        if( entry.Item != null )
                        {
                            Debug.Assert( entry.Item != e );
                            // If 2 items share the same full name, this is a structure error.
                            _computer.SetStructureError( entry, DependentItemStructureError.Homonym ).AddHomonym( e );
                            // Registers the homonym item to mark the item as processed.
                            _entries[e] = entry;
                            return entry;
                        }
                        // Item name is knwon (the entry exists), but the entry is not bound to the item.
                        // We bind the entry to its item.
                        Debug.Assert( entry.Requires != null && entry.Requires.Count > 0, "Already created by a RequiredBy." );
                        Debug.Assert( _entries[e] == startValue || _entries[e] == null, "The element is associated to the StartValue or to null (pure marker of preregistration)." );

                        // Associates the element to its entry.
                        _entries[e] = entry;
                        CreateOrInitEntry( ref entry, actualType, e, startValue );
                        #endregion
                    }
                    else
                    {
                        // The element nor its name has never been seen.
                        CreateOrInitEntry( ref entry, actualType, e, startValue );
                        _entries.Add( e.FullName, entry );
                    }
                    // We now have the Entry associated to its IDependentItem:
                    // we register it in the _result list (if it has been previously created
                    // to handle RequiredBy, it has not been registered).
                    _computer._result.Add( entry );
                    Debug.Assert( entry.Item == e );

                    // Now that the element has been registered, we can handle Requires and RequiredBy if they exist...
                    var requires = e.Requires;
                    if( requires != null )
                    {
                        foreach( IDependentItemRef r in requires )
                        {
                            var eR = r as IDependentItem;
                            if( eR != null ) RegisterEntry( eR, null, null );
                        }
                    }
                    var requiredBy = e.RequiredBy;
                    if( requiredBy != null )
                    {
                        foreach( IDependentItemRef reqBy in requiredBy )
                        {
                            Entry eReq = RegisterRequiredByDependency( reqBy );
                            eReq.AddRequiredByRequires( entry.Item.GetReference() );
                        }
                    }
                    // ...and Generalization...
                    var genRef = e.Generalization;
                    if( genRef != null )
                    {
                        // If it is an object (not a named reference), registers it
                        // but do not catch the resulting entry in entry.Generalization
                        // since it has yet to be fully resolved. 
                        // This is done after Container/Child binding.
                        var gen = e.Generalization as IDependentItem;
                        
                        // Support for "intrinsic optional object".
                        // Intrinsic optional objects are IDependentItem that implement IDependentItemRef and 
                        // for which Optional is true.
                        // The idea is that this kind of objects should NOT be automatically registered. 
                        // Generalization is currently the ONLY relationships that handles this kind of beast
                        // but it could be (I think) generalized to all relationships.
                        // Supporting these intrisically optional objects should be useful for easy (and dynamic) feature flipping.

                        if( gen != null && !genRef.Optional ) RegisterEntry( gen, null, null );
                        // SpecializedItems contains items and container (but no heads).
                        _specializedItems.Add( entry );
                    }

                    // ...and safely automatically discover its bound items: its container and its children.
                    //
                    bool handleItemContainer = e.Container != null;
                    // Starts with its Container :
                    // - first handle the case where we are called by a Group that claims to own the current element.
                    if( alreadyRegisteredGroup != null )
                    {
                        #region Call comes from one of our group or from a container.
                        if( alreadyRegisteredGroup.ItemKind == DependentItemKind.Container )
                        {
                            // We are coming from our container.
                            // If the item has a container, they must match.
                            if( e.Container != null )
                            {
                                // The current element has a container.
                                IDependentItemContainer father = e.Container as IDependentItemContainer;
                                if( alreadyRegisteredGroup.Item != father )
                                {
                                    if( father != null )
                                    {
                                        // The container differs from the one that contains it as a child.
                                        // Registers the container associated to the current element (to ensure auto-discovery).
                                        Entry extraContainer = RegisterEntry( father, null, entry );
                                        // ...and declares an error.
                                        // (Here we forget the fact that the extra container may be a IDependentItemContainerTyped where ItemKind != Container:
                                        // we consider the container mismatch as more important.)
                                        _computer.SetStructureError( entry, DependentItemStructureError.MultipleContainer ).AddExtraneousContainers( alreadyRegisteredGroup.Item.FullName );
                                    }
                                    else
                                    {
                                        // The container is a named reference.
                                        if( e.Container.FullName != alreadyRegisteredGroup.FullName )
                                        {
                                            // If it differs, declares an error.
                                            _computer.SetStructureError( entry, DependentItemStructureError.MultipleContainer ).AddExtraneousContainers( alreadyRegisteredGroup.FullName );
                                        }
                                    }
                                }
                            }
                            // Even if a structure error occured, we set the container.
                            alreadyRegisteredGroup.AddToContainer( entry );
                            handleItemContainer = false;
                        }
                        else
                        {
                            Debug.Assert( alreadyRegisteredGroup.ItemKind == DependentItemKind.Group, "A SimpleItem would not have called us." );
                            // Groups do not create any constraints. 
                            // Simply add the entry to the group.
                            alreadyRegisteredGroup.AddToGroup( entry );
                        }
                        #endregion
                    }
                    #region Now, handles item's Container if needed.
                    // If it declares a container, we try to bind to it.
                    if( handleItemContainer )
                    {
                        IDependentItemContainer father = e.Container as IDependentItemContainer;
                        if( father != null )
                        {
                            var cnt = RegisterEntry( father, null, entry );
                            if( cnt.ItemKind != DependentItemKind.Container )
                            {
                                // The container refused to be a container.
                                _computer.SetStructureError( entry, DependentItemStructureError.ExistingContainerAskedToNotBeAContainer );
                            }
                            else
                            {
                                if( entry.Container != null )
                                {
                                    if( entry.Container != cnt )
                                    {
                                        _computer.SetStructureError( entry, DependentItemStructureError.MultipleContainer ).AddExtraneousContainers( cnt.FullName );
                                    }
                                }
                                else
                                {
                                    cnt.AddToContainer( entry );
                                    Debug.Assert( entry.Container == cnt );
                                }
                            }
                        }
                        else _namedContainersToBind.Add( entry );
                    }
                    #endregion
                    #region Handles Groups.
                    // Whatever it is, if it declares Groups, handle them.
                    IEnumerable<IDependentItemGroupRef> groups;
                    if( (groups = e.Groups) != null )
                    {
                        foreach( IDependentItemGroupRef groupRef in groups )
                        {
                            // Skips null (security) and any reference to the group that is registering us.
                            if( groupRef == null || (alreadyRegisteredGroup != null && alreadyRegisteredGroup.FullName == groupRef.FullName) ) continue;
                            IDependentItemGroup group = groupRef as IDependentItemGroup;
                            if( group != null )
                            {
                                var gE = RegisterEntry( group, null, entry );
                                if( gE.ItemKind == DependentItemKind.Item )
                                {
                                    _computer.SetStructureError( entry, DependentItemStructureError.DeclaredGroupRefusedToBeAGroup ).AddInvalidGroup( gE.FullName );
                                }
                                else AddChildToGroupOrContainer( gE, entry );
                            }
                            else
                            {
                                _groupsToBind.Add( Tuple.Create( entry, groupRef ) );
                            }
                        }
                    }
                    #endregion
                    // If it is a group, handle its children. 
                    if( actualType == DependentItemKind.Item )
                    {
                        // If not, check if it is NOT a group that nevertheless exposes children: this must be 
                        // considered as an error to stay independent of the registration order.
                        if( g != null )
                        {
                            var children = g.Children;
                            if( children != null && children.Any() )
                            {
                                _computer.SetStructureError( entry, DependentItemStructureError.ContainerAskedToNotBeAGroupButContainsChildren );
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert( actualType != DependentItemKind.Item && g != null );
                        Debug.Assert( entry.HeadIfGroupOrContainer != null );
                        IEnumerable<IDependentItemRef> children;
                        if( (children = g.Children) != null )
                        {
                            IDependentItem knownChild = alreadyRegisteredChild != null ? alreadyRegisteredChild.Item : null;
                            foreach( IDependentItemRef childRef in children )
                            {
                                // Skips null by security.
                                if( childRef == null ) continue;

                                var child = childRef as IDependentItem;
                                if( child != null )
                                {
                                    // If the child is the child that calls us, then we are necessary a Container or a Group
                                    // and the child registration will manage the relation: we have nothing to do.
                                    if( child != knownChild )
                                    {
                                        // RegisterEntry handles the relation.
                                        RegisterEntry( child, entry, null );
                                    }
                                }
                                else
                                {
                                    // If the child is a named reference to the exact child that 
                                    // is calling us, we have nothing to do.
                                    if( knownChild == null || childRef.FullName != knownChild.FullName )
                                    {
                                        // The child must be late-bound.
                                        _childrenToBind.Add( Tuple.Create( entry, childRef ) );
                                    }
                                }
                            }
                        }
                    }
                    // The discoverer aspect of the item: its related elements are registered.
                    IDependentItemDiscoverer<T> disco = e as IDependentItemDiscoverer<T>;
                    if( disco != null )
                    {
                        var related = disco.GetOtherItemsToRegister();
                        if( related != null )
                        {
                            foreach( IDependentItem eR in related )
                            {
                                RegisterEntry( eR, null, null );
                            }
                        }
                    }
                    return entry;
                }

                Entry RegisterRequiredByDependency( IDependentItemRef r )
                {
                    var e = r as IDependentItem;
                    Entry entry;
                    if( e != null )
                    {
                        entry = RegisterEntry( e, null, null );
                    }
                    else
                    {
                        var strong = r.GetReference();
                        if( !TryGetEntryValue( strong.FullName, out entry ) )
                        {
                            // When an entry is created only to handle
                            // RequiredBy, we do not add it in _result.
                            entry = new Entry( _entries, strong.FullName );
                            _entries.Add( strong.FullName, entry );
                        }
                    }
                    return entry;
                }

                void CreateOrInitEntry( ref Entry entry, DependentItemKind actualType, T e, object startValue )
                {
                    if( entry == null )
                    {
                        entry = new Entry( _entries, e.FullName );
                        _entries[e] = entry;
                    }
                    if( entry.Init( e, actualType, startValue ) )
                    {
                        _entries.Add( entry.HeadIfGroupOrContainer.FullName, entry.HeadIfGroupOrContainer );
                        _computer._result.Add( entry.HeadIfGroupOrContainer );
                    }
                }

                /// <summary>
                /// Adapts the entries that is Dictionary(object,object) to behave like a Dictionary(object,Entry)
                /// by ignoring start values (that can not be an instance of the private Entry).
                /// </summary>
                bool TryGetEntryValue( object key, out Entry e )
                {
                    e = null;
                    object entryOrStartValue;
                    if( _entries.TryGetValue( key, out entryOrStartValue ) )
                    {
                        e = entryOrStartValue as Entry;
                    }
                    return e != null;
                }            
            }

            DependentItemIssue SetStructureError( Entry nc, DependentItemStructureError status )
            {
                DependentItemIssue issues = _itemIssues.Where( m => m.Item == nc.Item ).FirstOrDefault();
                if( issues == null ) _itemIssues.Add( (issues = new DependentItemIssue( nc.Item, status )) );
                else issues.StructureError |= status;
                return issues;
            }

            public int StartErrorCount => _startErrorCount;

            public bool FatalError => _fatalError;

            #region Processing: Rank computing & Cycle detection.

            public void Process()
            {
                if( _options.HookInput != null ) _options.HookInput( _entries.Where( e => e.Key is String ).Select( e => (Entry)e.Value ).Where( e => e.HeadIfGroupOrContainer == null ).Select( e => e.Item ) );
                // Note: Since we can NOT support dynamic resolution of a missing dependency
                // (through a function like ResolveMissing( fullName ) because of
                // the RequiredBy: if a newly added item has a RequiredBy, we should
                // reprocess all the items since it may change -a lot of- already computed rank),
                // the _result list does not change during process: we can safely foreach on it.
                foreach( var e in _result )
                {
                    if( e.Rank == -1 )
                    {
                        if( ComputeRank( e ) )
                        {
                            Debug.Assert( _cycle != null && _cycle.Count >= 2, "We started the cycle construction with the 2 first participants." );
                            if( _cycle[0].Relation != CycleExplainedElement.Start ) _cycle.Reverse();
                            break;
                        }
                    }
                }
            }

            bool ComputeRank( Entry e )
            {
                Debug.Assert( e.Rank == -1 );
                e.Rank = -2;
                int rank = 0;

                List<IDependentItemRef> requiresHiddenByContainerOrGen = null;

                // Prepares eGen.
                Entry eGen = e.Generalization;
                if( eGen == Entry.GeneralizationMissingMarker ) eGen = null;

                // Handle requirements (RequiredBy, Generalization and direct ones): 
                // this can be ignored for Groups since the Head handles them.
                //
                // Do this for Heads and Items (but not for Groups).
                //
                if( e.HeadIfGroupOrContainer == null )
                {
                    IEnumerable<IDependentItemRef> requirements;

                    // Starts with reverse requirements (RequiredBy) since during the registeration phasis,
                    // the Requires HashSet has been populated only with RequiredBy from others.
                    #region Reverse requirements (RequiredBy) handling.
                    requirements = e.GroupIfHead == null ? e.Requires : e.GroupIfHead.Requires;
                    if( requirements != null )
                    {
                        foreach( var dep in requirements )
                        {
                            Debug.Assert( _entries.ContainsKey( dep.FullName ) && ((Entry)_entries[dep.FullName]).Item != null, "Since the requirement has been added by an item, it exists." );
                            Entry oeDep = (Entry)_entries[dep.FullName];
                            if( (oeDep == eGen)
                                ||
                                (oeDep.ItemKind == DependentItemKind.Container && _options.SkipDependencyToContainer && e.AppearInContainerChain( oeDep )) )
                            {
                                if( requiresHiddenByContainerOrGen == null ) requiresHiddenByContainerOrGen = new List<IDependentItemRef>();
                                requiresHiddenByContainerOrGen.Add( dep );
                            }
                            else
                            {
                                if( HandleDependency( ref rank, e, CycleExplainedElement.RequiredByRequires, oeDep ) ) return true;
                            }
                        }
                    }
                    #endregion
                    
                    // We then handle Generalization as a requirement.
                    if( eGen != null )
                    {
                        if( HandleDependency( ref rank, e, CycleExplainedElement.GeneralizedBy, eGen ) ) return true;
                    }

                    #region Handles direct requirements
                    requirements = e.Item.Requires;
                    if( requirements != null )
                    {
                        foreach( IDependentItemRef dep in requirements )
                        {
                            // Security: skips any null entry and Generalization if it exists.
                            if( dep == null || (eGen != null && (eGen == dep || eGen.FullName == dep.FullName)) ) continue;

                            // Creates the HashSet only if needed.
                            if( e.Requires == null ) e.Requires = new HashSet<IDependentItemRef>();
                            IDependentItemRef strong = dep.GetReference();
                            Debug.Assert( ReferenceEquals( dep, strong ) == !dep.Optional );

                            // Adds the dependency to the HashSet: if it already exists, this requirement
                            // has already been handled either through a RequiredBy or by a previous
                            // duplicate in this requirements.
                            // The trick here is to first consider the non-optional dependency name (dep that does not start with ?).
                            // If the strong requirement has been processed, it is useless to process it again, be it a strong or 
                            // an optional requirement.
                            // Second, we add the raw dependency (can be optional or strong): if the exact form has already been added,
                            // it is useless to continue.
                            //  
                            // ?Dep =>  We add "?Dep" to the Requires and 
                            //          - If the dependency exists, it is fullfilled, we also add the "Dep":
                            //            if "Dep" occurs later, nothing is done.
                            //          - If the dependency is missing, we add "?Dep" to the missing, but we can not 
                            //            consider that "Dep" has been processed since, if it occurs later, we must be 
                            //            able to register it as a "strong" missing, overriding the first optional one.
                            // Dep =>   We add "Dep" to the Requires and
                            //          - If the dependency exists, we do not add "?Dep" since
                            //            if "?Dep" occurs later, it will first test for "Dep" (useless to pollute the Requires).
                            //          - If the dependency is missing, we add the "?Dep" form to the Requires: this 
                            //            avoids us to process "?Dep" and have the (considered positive) side effect of hiding 
                            //            the optionnal requirement in the final Missing collector.
                            if( !e.Requires.Contains( strong ) && e.Requires.Add( dep ) )
                            {
                                object oEntry;
                                Entry oeDep;
                                if( !_entries.TryGetValue( strong.FullName, out oEntry ) || (oeDep = (Entry)oEntry).Item == null )
                                {
                                    SetStructureError( e, dep.Optional ? DependentItemStructureError.None : DependentItemStructureError.MissingDependency ).AddMissing( dep );
                                    if( !dep.Optional ) e.Requires.Add( dep.GetOptionalReference() );
                                }
                                else
                                {
                                    // Adds the strong dependency to mark it as processed even if it will be removed by skipDependencyToContainer.
                                    if( dep.Optional ) e.Requires.Add( strong );
                                    if( oeDep.ItemKind == DependentItemKind.Container
                                        && _options.SkipDependencyToContainer
                                        && e.AppearInContainerChain( oeDep ) )
                                    {
                                        if( requiresHiddenByContainerOrGen == null ) requiresHiddenByContainerOrGen = new List<IDependentItemRef>();
                                        requiresHiddenByContainerOrGen.Add( dep );
                                    }
                                    else
                                    {
                                        if( HandleDependency( ref rank, e, CycleExplainedElement.Requires, oeDep ) ) return true;
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    //
                    // Group or Container only.
                    //
                    // Handles Group => Head (useful for empty groups).
                    Debug.Assert( e.HeadIfGroupOrContainer != null );
                    if( HandleDependency( ref rank, e, CycleExplainedElement.Start, e.HeadIfGroupOrContainer ) ) return true;
                }

                // Handles the element's Container: its head is required by this item (be it a head, a container, a group or an item).
                if( e.Container != null )
                {
                    // Checks (Debug only) that an element that claims to belong to a Container
                    // is actually in the linked list of its children.
                    e.Container.CheckContainerIsHeadOrContains( e );

                    if( HandleDependency( ref rank, e, CycleExplainedElement.ElementOfContainer, e.Container.HeadIfGroupOrContainer ) ) return true;
                }
                // Handles the element's Groups: their heads are required by this item (be it a head, a container, a group or an item).
                var groups = e.GroupIfHead != null ? e.GroupIfHead.Groups : e.Groups;
                if( groups != null )
                {
                    foreach( Entry group in groups )
                    {
                        Debug.Assert( group.HeadIfGroupOrContainer != null && group.GroupChildren.Contains( e.GroupIfHead ?? e ) );
                        if( HandleDependency( ref rank, e, CycleExplainedElement.ElementOf, group.HeadIfGroupOrContainer ) ) return true;
                    }
                }
                // Handles group children if any.
                if( e.GroupChildren != null )
                {
                    foreach( Entry child in e.GroupChildren )
                    {
                        if( HandleDependency( ref rank, e, CycleExplainedElement.Contains, child ) ) return true;
                    }
                }
                else
                {
                    // Handles container's children if any.
                    Entry child = e.FirstChildIfContainer;
                    while( child != null )
                    {
                        if( HandleDependency( ref rank, e, CycleExplainedElement.ContainerContains, child ) ) return true;
                        child = child.NextChildInContainer;
                    }
                }
                // Should the entry.Requires HashSet be cleaned up?
                if( requiresHiddenByContainerOrGen != null )
                {
                    Debug.Assert( requiresHiddenByContainerOrGen.Count > 0 );
                    Debug.Assert( e.Requires != null );
                    foreach( var hiddenDep in requiresHiddenByContainerOrGen )
                    {
                        e.Requires.Remove( hiddenDep );
                    }
                }
                // The rank of the item is known now.
                e.Rank = rank + 1;
                return false;
            }

            bool HandleDependency( ref int rank, Entry e, char relation, Entry oeDep )
            {
                // Cycle detection.
                if( oeDep.Rank == -2 )
                {
                    if( relation != CycleExplainedElement.Start ) StartCycle( e, relation, oeDep );
                     return true;
                }
                if( oeDep.Rank == -1 )
                {
                    if( ComputeRank( oeDep ) )
                    {
                        // We let the first and last item be the same in the cycle since it is much more
                        // explicit for users: instead of a=>b=>c, they can see a=>b=>c=>a and a cycle
                        // has necessarily a length of at least 2.
                        // Autoreferences are cycles like a=>a.
                        //
                        // Do not add "Container" => "Head". It is useless and hard 
                        // to understand since the name is the same ("X" => "X").
                        // We do not test that oeDep is the head of the item (either by its name
                        // or by changing the parameter to the entry) in order to not 
                        // mask auto-dependency.
                        if( relation != CycleExplainedElement.Start )
                        {
                            if( _cycle == null ) StartCycle( e, relation, oeDep );
                            else
                            {
                                Debug.Assert( _cycle.Count >= 2 );
                                // Is the cycle already fully detected?
                                if( _cycle[0].Relation != CycleExplainedElement.Start )
                                {
                                    Debug.Assert( _cycle[_cycle.Count - 1].Item == (oeDep.GroupIfHead ?? oeDep).Item, "Last one is the target of the relation." );

                                    if( e.GroupIfHead != null ) e = e.GroupIfHead;
                                    // Updates relation to the last one.
                                    _cycle[_cycle.Count - 1].Relation = relation;
                                    _cycle.Add( new CycleExplainedElement( CycleExplainedElement.Start, e.Item ) );
                                    // Should we stop the detection ?
                                    if( _cycle[0].Item == e.Item )
                                    {
                                        _cycle.Reverse();
                                    }
                                }
                            }
                        }
                        return true;
                    }
                }
                rank = Math.Max( rank, oeDep.Rank );
                return false;
            }

            private void StartCycle( Entry e, char relation, Entry oeDep )
            {
                Debug.Assert( relation != CycleExplainedElement.Start );
                if( oeDep.GroupIfHead != null ) oeDep = oeDep.GroupIfHead;
                if( e.GroupIfHead != null ) e = e.GroupIfHead;
                _cycle = new List<CycleExplainedElement>() { new CycleExplainedElement( relation, oeDep.Item ), new CycleExplainedElement( CycleExplainedElement.Start, e.Item ) };
                // Auto reference? Immediately ends the cycle.
                if( e == oeDep ) _cycle.Reverse();
            }

            public DependencySorterResult<T> GetResult()
            {
                if( _cycle == null )
                {
                    _result.Sort( _comparer );
                    int i = 0;
                    foreach( var e in _result ) e.Index = i++;
                    if( _options.HookOutput != null ) _options.HookOutput( _result );
                    return new DependencySorterResult<T>( _result, null, _itemIssues, _startErrorCount, _fatalError );
                }
                return new DependencySorterResult<T>( null, _cycle, _itemIssues, _startErrorCount, _fatalError );
            }

            static int NormalComparer( Entry o1, Entry o2 )
            {
                int cmp = o1.Rank - o2.Rank;
                if( cmp == 0 ) cmp = o1.FullName.CompareTo( o2.FullName );
                return cmp;
            }

            static int ReverseComparer( Entry o1, Entry o2 )
            {
                int cmp = o1.Rank - o2.Rank;
                if( cmp == 0 ) cmp = o2.FullName.CompareTo( o1.FullName );
                return cmp;
            }

            #endregion
        }

    }
}
