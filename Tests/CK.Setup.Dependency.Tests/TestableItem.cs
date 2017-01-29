#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\TestableItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using CK.Core;

namespace CK.Setup.Dependency.Tests
{
    class TestableItem : IDependentItem, IDependentItemDiscoverer, IDependentItemRef
    {
        string _fullName;
        List<IDependentItemRef> _requires;
        List<IDependentItemRef> _requiredBy;
        List<IDependentItemGroupRef> _groups;
        List<IDependentItem> _relatedItems;
        int _startDependencySortCount;

        static int _ignoreCheckedCount = 0;
        public static IDisposable IgnoreCheckCount()
        {
            ++_ignoreCheckedCount;
            return Util.CreateDisposableAction( () => --_ignoreCheckedCount );
        }

        public TestableItem( string fullName, params object[] content )
        {
            _requires = new List<IDependentItemRef>();
            _requiredBy = new List<IDependentItemRef>();
            _groups = new List<IDependentItemGroupRef>();
            FullName = fullName;
            if( content != null ) Add( content );
            if( _ignoreCheckedCount > 0 ) _startDependencySortCount = -1;
        }

        public virtual void Add( params object[] content )
        {
            if( content == null ) return;
            foreach( object o in content )
            {
                if( o != null && o is string )
                {
                    string dep = (string)o;
                    if( !HandleItemString( dep ) )
                    {
                        throw new ArgumentException( "Only RequiredBy (↽), Requires (⇀), GeneralizedBy (↟), ElementOfContainer (⊏) and ElementOf (∈) are supported." );
                    }
                }
            }
        }

        protected bool HandleItemString( string dep )
        {
            if( dep[0] == CycleExplainedElement.RequiredBy ) // ↽
            {
                _requiredBy.Add( new NamedDependentItemRef( dep.Substring( 1 ).Trim() ) );
            }
            else if( dep[0] == CycleExplainedElement.Requires ) // ⇀
            {
                _requires.Add( new NamedDependentItemRef( dep.Substring( 1 ).Trim() ) );
            }
            else if( dep[0] == CycleExplainedElement.ElementOfContainer ) // ⊏
            {
                Container = new NamedDependentItemContainerRef( dep.Substring( 1 ).Trim() );
            }
            else if( dep[0] == CycleExplainedElement.ElementOf ) // ∈
            {
                _groups.Add( new NamedDependentItemGroupRef( dep.Substring( 1 ).Trim() ) );
            }
            else if( dep[0] == CycleExplainedElement.GeneralizedBy ) // ↟
            {
                Generalization = new NamedDependentItemRef( dep.Substring( 1 ).Trim() );
            }
            else
            {
                return false;
            }
            return true;
        }

        public IDependentItemContainerRef Container { get; set; }

        public IDependentItemRef Generalization { get; set; }

        public void CheckStartDependencySortCountAndReset()
        {
            if( _startDependencySortCount != -1 )
            {
                Assert.That( _startDependencySortCount, Is.EqualTo( 1 ), "StartDependencySort must have been called once and only once." );
                _startDependencySortCount = 0;
            }
        }

        public string FullName 
        {
            get 
            {
                if( _startDependencySortCount != -1 )
                {
                    Assert.That( _startDependencySortCount, Is.EqualTo( 1 ), "StartDependencySort must have been called once and only once." );
                }
                return _fullName; 
            }
            set { _fullName = value; } 
        }

        public IList<IDependentItemRef> Requires { get { return _requires; } }

        public IList<IDependentItemRef> RequiredBy { get { return _requiredBy; } }

        public IList<IDependentItemGroupRef> Groups { get { return _groups; } }

        IEnumerable<IDependentItemRef> IDependentItem.Requires { get { return _requires; } }

        IEnumerable<IDependentItemRef> IDependentItem.RequiredBy { get { return _requiredBy; } }

        IEnumerable<IDependentItemGroupRef> IDependentItem.Groups { get { return _groups; } }

        public IList<IDependentItem> RelatedItems
        {
            get { return _relatedItems ?? (_relatedItems = new List<IDependentItem>()); }
        }

        IEnumerable<IDependentItem> IDependentItemDiscoverer<IDependentItem>.GetOtherItemsToRegister()
        {
            return _relatedItems;
        }

        public override string ToString()
        {
            return FullName;
        }

        bool IDependentItemRef.Optional
        {
            get { return false; }
        }

        object IDependentItem.StartDependencySort( IActivityMonitor m )
        {
            if( _startDependencySortCount != -1 )
            {
                Assert.That( _startDependencySortCount, Is.EqualTo( 0 ), "StartDependencySort must be called once and only once." );
                ++_startDependencySortCount;
            }
            return _startDependencySortCount;
        }
    }

}
