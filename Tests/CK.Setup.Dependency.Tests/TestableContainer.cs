#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\TestableContainer.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup.Dependency.Tests
{
    class TestableContainer : TestableItem, IDependentItemContainerTyped, IDependentItemContainerRef
    {
        List<IDependentItemRef> _children = new List<IDependentItemRef>();

        public TestableContainer( string fullName, params object[] content )
            : base( fullName, content )
        {
            ItemKind = DependentItemKind.Container;
        }

        public TestableContainer( DependentItemKind dynamicType, string fullName, params object[] content )
            : base( fullName, null )
        {
            ItemKind = dynamicType;
            Add( content );
        }

        public DependentItemKind ItemKind { get; private set; }

        IEnumerable<IDependentItemRef> IDependentItemGroup.Children
        {
            get { return _children; }
        }

        public IList<IDependentItemRef> Children
        {
            get { return _children; }
        }

        public override void Add( params object[] content )
        {
            foreach( object o in content )
            {
                TestableItem i = o as TestableItem;
                if( i != null )
                {
                    _children.Add( i );
                    if( ItemKind == DependentItemKind.Container || ItemKind == DependentItemKind.Unknown ) i.Container = this;
                }
                else
                {
                    string dep = (string)o;
                    if( dep[0] == CycleExplainedElement.ContainerContains ) // ⊐
                    {
                        if( ItemKind != DependentItemKind.Container && ItemKind != DependentItemKind.Unknown )
                        {
                            throw new ArgumentException( "ContainerContains (⊐) must be used only when ItemKind = Container. Use Contains (∋) to add an element in a Group." );
                        }
                        _children.Add( new NamedDependentItemRef( dep.Substring( 1 ).Trim() ) );
                    }
                    else if( dep[0] == CycleExplainedElement.Contains ) // ∋
                    {
                        if( ItemKind == DependentItemKind.Container || ItemKind == DependentItemKind.Unknown )
                        {
                            throw new ArgumentException( "Contains (∋)  must be used only when ItemKind != Container. Use ContainerContains (⊐) to add an element in a Container." );
                        }
                        _children.Add( new NamedDependentItemRef( dep.Substring( 1 ).Trim() ) );
                    }
                    else if( !HandleItemString( dep ) )
                    {
                        _children.Add( new NamedDependentItemRef( dep ) );
                    }
                }
            }
        }

    }
}
