#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setup.Dependency\Sorter\DependentItemIssue.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.IO;

namespace CK.Setup
{
    /// <summary>
    /// Describes an error in the structure for an <see cref="Item"/> like missing dependencies, homonyms, etc. 
    /// </summary>
    public sealed class DependentItemIssue
    {
        string[] _missingDep;
        string[] _extraneousContainers;
        string[] _invalidGroups;
        string[] _missingChildren;
        string[] _missingGroups;
        IDependentItem[] _homonyms;
        int _nbRequiredMissingDep;
        
        /// <summary>
        /// Constructor for a missing named container or other structure errors.
        /// This may be called with a <see cref="DependentItemStructureError.None"/>
        /// status to register the very first optional missing dependency.
        /// </summary>
        internal DependentItemIssue( IDependentItem item, DependentItemStructureError m )
        {
            Item = item;
            StructureError = m;
        }

        internal void AddHomonym( IDependentItem homonym )
        {
            Debug.Assert( homonym != null );
            Debug.Assert( Item != homonym && Item.FullName == homonym.FullName );
            Append( ref _homonyms, homonym );
        }

        internal void AddMissing( IDependentItemRef dep )
        {
            Debug.Assert( !String.IsNullOrWhiteSpace( dep.FullName ) );
            string missing = dep.FullName;
            if( dep.Optional ) missing = '?' + missing;
            if( _missingDep == null )
            {
                _missingDep = new[] { missing };
            }
            else
            {
                Debug.Assert( Array.IndexOf( _missingDep, missing ) < 0, "Duplicates are handled by ComputeRank." );
                int len = _missingDep.Length;
                // This is to maintain the fact that a strong missing 
                // dependency hides an optional one.
                if( !dep.Optional )
                {
                    string weak = '?' + missing;
                    int idx = Array.IndexOf( _missingDep, weak );
                    if( idx >= 0 )
                    {
                        StructureError |= DependentItemStructureError.MissingDependency;
                        ++_nbRequiredMissingDep;
                        _missingDep[idx] = missing;
                        return;
                    }
                }
                Array.Resize( ref _missingDep, len + 1 );
                _missingDep[len] = missing;
            }
            if( !dep.Optional )
            {
                StructureError |= DependentItemStructureError.MissingDependency;
                ++_nbRequiredMissingDep;
            }
        }

        internal void AddExtraneousContainers( string name )
        {
            Append( ref _extraneousContainers, name );
        }

        internal void AddInvalidGroup( string name )
        {
            Append( ref _invalidGroups, name );
        }

        internal void AddMissingChild( string name )
        {
            Append( ref _missingChildren, name );
        }

        internal void AddMissingGroup( string name )
        {
            Append( ref _missingGroups, name );
        }

        private void Append<T>( ref T[] a, T e )
        {
            if( a == null ) a = new T[] { e };
            else
            {
                int len = a.Length;
                Array.Resize( ref a, len + 1 );
                a[len] = e;
            }
        }

        /// <summary>
        /// Gets the total count of missing items (required dependencies and generalization if any).
        /// </summary>
        public int RequiredMissingCount
        {
            get 
            { 
                int r = _nbRequiredMissingDep;
                if( (StructureError & DependentItemStructureError.MissingGeneralization) != 0 ) r += 1;
                return r;
            }
        }

        /// <summary>
        /// The item for which this issue exists.
        /// </summary>
        public readonly IDependentItem Item;

        /// <summary>
        /// Gets a bit flag that summarizes the different errors related to structure 
        /// </summary>
        public DependentItemStructureError StructureError { get; internal set; }

        /// <summary>
        /// Dumps this issue to the monitor.
        /// </summary>
        /// <param name="monitor">Monitor to use. Must not be null.</param>
        public void LogError( IActivityMonitor monitor )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            if( StructureError != DependentItemStructureError.None )
            {
                using( monitor.OpenInfo().Send( "Errors on '{0}'", Item.FullName ) )
                {
                    if( (StructureError & DependentItemStructureError.MissingNamedContainer) != 0 )
                    {
                        monitor.Error().Send( "Missing container named '{0}'", Item.Container.FullName );
                    }
                    if( (StructureError & DependentItemStructureError.ExistingItemIsNotAContainer) != 0 )
                    {
                        monitor.Error().Send( "Items's container named '{0}' is not a container.", Item.Container.FullName );
                    }
                    if( (StructureError & DependentItemStructureError.ExistingContainerAskedToNotBeAContainer) != 0 )
                    {
                        monitor.Error().Send( "Items's container '{0}' dynamically states that it is actually not a container. (Did you forget to configure the ItemKind of the object? This can be done for instance with the attribute [StObj( ItemKind = DependentItemType.Container )].)", Item.Container.FullName );
                    }
                    if( (StructureError & DependentItemStructureError.ContainerAskedToNotBeAGroupButContainsChildren) != 0 )
                    {
                        monitor.Error().Send( "Potential container '{0}' dynamically states that it is actually not a Container nor a Group but contains Children. (Did you forget to configure the ItemKind of the object? When IDependentItemContainerTyped.ItemKind is SimpleItem, the Children enumeration must be null or empty. This can be done for instance with the attribute [StObj( ItemKind = DependentItemType.Container )].)", Item.FullName );
                    }
                    if( (StructureError & DependentItemStructureError.MissingGeneralization) != 0 )
                    {
                        monitor.Error().Send( "Item '{0}' requires '{1}' as its Generalization. The Generalization is missing.", Item.FullName, Item.Generalization.FullName );
                    }
                    if( (StructureError & DependentItemStructureError.DeclaredGroupRefusedToBeAGroup) != 0 )
                    {
                        monitor.Error().Send( "Item '{0}' declares Groups that states that they are actually not Groups (their ItemKind is SimpleItem): '{1}'.", Item.FullName, String.Join( "', '", _invalidGroups ) );
                    }
                    if( (StructureError & DependentItemStructureError.MissingNamedGroup) != 0 )
                    {
                        monitor.Error().Send( "Item '{0}' declares required Groups that are not registered: '{1}'. ", Item.FullName, String.Join( "', '", _missingGroups ) );
                    }
                    if( _homonyms != null )
                    {
                        monitor.Error().Send( "Homonyms: {0} objects with the same full name.", _homonyms.Length );
                    }
                    if( _extraneousContainers != null )
                    {
                        if( Item.Container != null )
                        {
                            monitor.Error().Send( "This item states to belong to container '{0}', but other containers ('{1}') claim to own it.", Item.Container.FullName, String.Join( "', '", _extraneousContainers ) );
                        }
                        else
                        {
                            monitor.Error().Send( "More than one container claim to own the item: '{0}'.", String.Join( "', '", _extraneousContainers ) );
                        }
                    }
                    if( _missingChildren != null )
                    {
                        monitor.Error().Send( "Missing children items: '{0}'.", String.Join( "', '", _missingChildren ) );
                    }
                    if( _nbRequiredMissingDep > 0 )
                    {
                        monitor.Error().Send( "Missing required dependencies: '{0}'.", String.Join( "', '", _missingDep.Where( s => s[0] != '?' ) ) );
                    }
                }
            }
        }

        /// <summary>
        /// Gets the list of conflicting containers if any. Never null.
        /// </summary>
        public IEnumerable<string> ExtraneousContainers => _extraneousContainers ?? Util.Array.Empty<string>();

        /// <summary>
        /// Gets the list of <see cref="IDependentItem"/> that share the same name. Never null.
        /// </summary>
        public IEnumerable<IDependentItem> Homonyms => _homonyms ?? Util.Array.Empty<IDependentItem>();

        /// <summary>
        /// Gets the list of missing children if any (when named references are used). Never null.
        /// </summary>
        public IEnumerable<string> MissingChildren => _missingChildren ?? Util.Array.Empty<string>();

        /// <summary>
        /// Gets a list of missing dependencies either optional (starting with '?') or required. 
        /// Use <see cref="RequiredMissingCount"/> to know if required dependencies exist. 
        /// It is never null and there are no duplicates in this list and a required dependency "hides" an optional one:
        /// if a dependency is both required and optional, only the required one appears in this list.
        /// </summary>
        public IEnumerable<string> MissingDependencies => _missingDep ?? Util.Array.Empty<string>();

        /// <summary>
        /// Gets a list of required missing dependencies for this <see cref="Item"/>. 
        /// Null if <see cref="RequiredMissingCount"/> is 0.
        /// </summary>
        public IEnumerable<string> RequiredMissingDependencies
        {
            get { return _nbRequiredMissingDep > 0 ? _missingDep.Where( s => s[0] != '?' ) : null; }
        }

        /// <summary>
        /// Overridden to use <see cref="LogError"/>.
        /// </summary>
        /// <returns>The text of the dump.</returns>
        public override string ToString()
        {
            if( StructureError != DependentItemStructureError.None )
            {
                TextWriter writer = new StringWriter();
                var m = new ActivityMonitor( false );
                m.Output.RegisterClient( new ActivityMonitorErrorCounter( generateConclusion: true ) );
                m.Output.RegisterClient( new ActivityMonitorTextWriterClient( writer.Write ) );
                LogError( m );
                return writer.ToString();
            }
            return "(no error)";
        }

    }

}
