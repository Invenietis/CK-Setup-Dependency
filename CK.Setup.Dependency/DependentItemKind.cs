#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setup.Dependency\DependentItemKind.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion


namespace CK.Setup
{
    /// <summary>
    /// Describes the kind of a <see cref="IDependentItem"/>.
    /// Used by <see cref="IDependentItemContainerTyped"/> to dynamically restrict its type.
    /// </summary>
    public enum DependentItemKind
    {
        /// <summary>
        /// Unknown type can be used for instance to dynamically adjust the behavior of the item.
        /// </summary>
        Unknown,

        /// <summary>
        /// Considers the item as a pure <see cref="IDependentItem"/>.
        /// </summary>
        Item,

        /// <summary>
        /// Considers the item as a <see cref="IDependentItemGroup"/>.
        /// </summary>
        Group,

        /// <summary>
        /// Considers the item as a <see cref="IDependentItemContainer"/>.
        /// </summary>
        Container
    }

}
