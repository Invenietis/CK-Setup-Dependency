#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\ItemAndItemRef.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;

namespace CK.Setup.Dependency.Tests
{
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
            
            IDependentItemRef refItem = item.GetReference();
            IDependentItemGroupRef refGroup = group.GetReference();
            IDependentItemContainerRef refContainer = container.GetReference();

            Assert.That( refItem.FullName, Is.EqualTo( "Item" ) );
            Assert.That( refGroup.FullName, Is.EqualTo( "Group" ) );
            Assert.That( refContainer.FullName, Is.EqualTo( "Container" ) );
            Assert.That( refItem.Optional, Is.False );
            Assert.That( refGroup.Optional, Is.False );
            Assert.That( refContainer.Optional, Is.False );

            IDependentItemRef refItemO = refItem.GetOptionalReference();
            IDependentItemGroupRef refGroupO = refGroup.GetOptionalReference();
            IDependentItemContainerRef refContainerO = refContainer.GetOptionalReference();

            Assert.That( refItemO.FullName, Is.EqualTo( "Item" ) );
            Assert.That( refGroupO.FullName, Is.EqualTo( "Group" ) );
            Assert.That( refContainerO.FullName, Is.EqualTo( "Container" ) );
            Assert.That( refItemO.Optional, Is.True );
            Assert.That( refGroupO.Optional, Is.True );
            Assert.That( refContainerO.Optional, Is.True );

            IDependentItemRef refItem2 = refItemO.GetReference();
            IDependentItemGroupRef refGroup2 = refGroupO.GetReference();
            IDependentItemContainerRef refContainer2 = refContainerO.GetReference();
            Assert.That( refItem2.FullName, Is.EqualTo( "Item" ) );
            Assert.That( refGroup2.FullName, Is.EqualTo( "Group" ) );
            Assert.That( refContainer2.FullName, Is.EqualTo( "Container" ) );
            Assert.That( refItem2.Optional, Is.False );
            Assert.That( refGroup2.Optional, Is.False );
            Assert.That( refContainer2.Optional, Is.False );
        }
    }
}
