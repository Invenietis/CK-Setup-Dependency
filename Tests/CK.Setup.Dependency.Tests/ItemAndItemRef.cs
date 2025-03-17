#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\ItemAndItemRef.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using NUnit.Framework;
using CK.Core;
using Shouldly;

namespace CK.Setup.Dependency.Tests;

[TestFixture]
public class ItemAndItemRef
{
    class Item : IDependentItem, IDependentItemRef
    {
        public string FullName { get; set; }

        public bool Optional { get; set; }

        public IDependentItemContainerRef Container
        {
            get { throw new NotImplementedException(); }
        }

        public IDependentItemRef Generalization
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<IDependentItemRef> Requires
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<IDependentItemRef> RequiredBy
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<IDependentItemGroupRef> Groups
        {
            get { throw new NotImplementedException(); }
        }

        public object StartDependencySort( IActivityMonitor m )
        {
            throw new NotImplementedException();
        }

    }

    class Group : Item, IDependentItemGroup, IDependentItemGroupRef
    {
        public IEnumerable<IDependentItemRef> Children
        {
            get { throw new NotImplementedException(); }
        }
    }

    class Container : Group, IDependentItemContainer, IDependentItemContainerRef
    {
    }

    [Test]
    public void GetReferences()
    {
        IDependentItem item = new Item() { FullName = "Item" };
        IDependentItemGroup group = new Group() { FullName = "Group" };
        IDependentItemContainer container = new Container() { FullName = "Container" };

        IDependentItemRef refItem = item.GetReference().ShouldNotBeNull();
        IDependentItemGroupRef refGroup = group.GetReference().ShouldNotBeNull();
        IDependentItemContainerRef refContainer = container.GetReference().ShouldNotBeNull();

        refItem.FullName.ShouldBe("Item" );
        refGroup.FullName.ShouldBe( "Group" );
        refContainer.FullName.ShouldBe( "Container" );
        refItem.Optional.ShouldBeFalse();
        refGroup.Optional.ShouldBeFalse();
        refContainer.Optional.ShouldBeFalse();

        IDependentItemRef refItemO = refItem.GetOptionalReference().ShouldNotBeNull();
        IDependentItemGroupRef refGroupO = refGroup.GetOptionalReference().ShouldNotBeNull();
        IDependentItemContainerRef refContainerO = refContainer.GetOptionalReference().ShouldNotBeNull();

        refItemO.FullName.ShouldBe( "Item" );
        refGroupO.FullName.ShouldBe( "Group" );
        refContainerO.FullName.ShouldBe( "Container" );
        refItemO.Optional.ShouldBeTrue();
        refGroupO.Optional.ShouldBeTrue();
        refContainerO.Optional.ShouldBeTrue();

        IDependentItemRef refItem2 = refItemO.GetReference().ShouldNotBeNull();
        IDependentItemGroupRef refGroup2 = refGroupO.GetReference().ShouldNotBeNull();
        IDependentItemContainerRef refContainer2 = refContainerO.GetReference().ShouldNotBeNull();
        refItem2.FullName.ShouldBe( "Item" );
        refGroup2.FullName.ShouldBe( "Group" );
        refContainer2.FullName.ShouldBe( "Container" );
        refItem2.Optional.ShouldBeFalse();
        refGroup2.Optional.ShouldBeFalse();
        refContainer2.Optional.ShouldBeFalse();
    }
}
