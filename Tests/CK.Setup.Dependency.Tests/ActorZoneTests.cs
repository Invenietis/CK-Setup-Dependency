#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\ActorZoneTests.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using System.Reflection;

namespace CK.Setup.Dependency.Tests
{
    [TestFixture]
    public class ActorZoneTests
    {

        [Test]
        public void LayeredArchitecture()
        {
            var basicPackage = new TestableContainer( "BasicPackage" );
            var basicActor = new TestableContainer( DependentItemKind.Item, "BasicActor" );
            var basicGroup = new TestableContainer( DependentItemKind.Item, "BasicGroup" );
            var zonePackage = new TestableContainer( "ZonePackage" );
            var zoneGroup = new TestableContainer( DependentItemKind.Item, "ZoneGroup" );
            var securityZone = new TestableContainer( DependentItemKind.Item, "SecurityZone" );
            var sqlDatabaseDefault = new TestableContainer( DependentItemKind.Group, "SqlDatabaseDefault" );

            sqlDatabaseDefault.Add( basicPackage, basicActor, basicGroup, zonePackage, zoneGroup, securityZone );
            basicActor.Container = basicPackage;
            basicGroup.Container = basicPackage;
            basicGroup.Requires.Add( basicActor );
            zonePackage.Generalization = basicPackage;
            zoneGroup.Generalization = basicGroup;
            zoneGroup.Container = zonePackage;
            zoneGroup.Requires.Add( securityZone );
            securityZone.Container = zonePackage;
            securityZone.Requires.Add( basicGroup );

            {
                var r = DependencySorter.OrderItems(
                    TestHelper.ConsoleMonitor,
                    new DependencySorterOptions()
                    {
                        HookInput = TestHelper.Trace,
                        HookOutput = sortedItems => TestHelper.Trace( sortedItems, false )
                    }, 
                    sqlDatabaseDefault, basicPackage, basicActor, basicGroup, zonePackage, zoneGroup, securityZone );
                Assert.That( r.IsComplete );
                r.AssertOrdered( "SqlDatabaseDefault.Head", "BasicPackage.Head", "BasicActor", "BasicGroup", "BasicPackage", "ZonePackage.Head", "SecurityZone", "ZoneGroup", "ZonePackage", "SqlDatabaseDefault" );  
                ResultChecker.SimpleCheck( r );
                r.CheckChildren( "BasicPackage", "BasicActor,BasicGroup" );
                r.CheckChildren( "ZonePackage", "ZoneGroup,SecurityZone" );
                r.CheckChildren( "SqlDatabaseDefault", "BasicPackage,BasicActor,BasicGroup,ZonePackage,ZoneGroup,SecurityZone" );
            }
        }
    }
}
