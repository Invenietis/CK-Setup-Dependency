#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setup.Dependency\Sorter\CycleExplainedElement.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Describes a relation from an origin item to another one in a cycle.
    /// </summary>
    public interface ICycleExplainedElement
    {
        /// <summary>
        /// Gets one of the relationship character defined in <see cref="CycleExplainedElement"/>.
        /// </summary>
        char Relation { get; }

        /// <summary>
        /// Gets the target item.
        /// </summary>
        IDependentItem Item { get; }
    }

    /// <summary>
    /// Implements <see cref="ICycleExplainedElement"/> and defines relationship types as characters.
    /// </summary>
    public class CycleExplainedElement : ICycleExplainedElement
    {
        /// <summary>
        /// First item of the cycle: the last one is the same as this one ('↳' - Unicode: \u21B3).
        /// </summary>
        public readonly static char Start                   = '↳';  // Unicode: \u21B3
        
        /// <summary>
        /// Previous item belongs to a container ('∈' - Unicode \u2208).
        /// </summary>
        public readonly static char ElementOf               = '∈';  // Unicode \u2208
        
        /// <summary>
        /// Previous item is a container that contains an item ('∋' - Unicode \u220B).
        /// </summary>
        public readonly static char Contains                = '∋';  // Unicode: \u220B
        /// <summary>
        /// Previous item requires an item ('⇀' - Unicode \u21C0).
        /// </summary>
        public readonly static char Requires                = '⇀';  // Unicode: \u21C0
        /// <summary>
        /// Previous item is required by an item ('↽' - Unicode: \u21BD).
        /// </summary>
        public readonly static char RequiredBy              = '↽';  // Unicode: \u21BD

        /// <summary>
        /// Previous item is required by and also requires an item ('⇌' - Unicode \u21CC).
        /// </summary>
        public readonly static char RequiredByRequires      = '⇌';  // Unicode: \u21CC

        /// <summary>
        /// Previous item is a container that belongs to a container ('⊏' - Unicode \u228F).
        /// </summary>
        public readonly static char ElementOfContainer      = '⊏';  // Unicode: \u228F

        /// <summary>
        /// Previous item is a container that contains a container ('⊐' - Unicode \u2290)
        /// </summary>
        public readonly static char ContainerContains       = '⊐';  // Unicode: \u2290
        
        /// <summary>
        /// Previous item is a specialization of an item ('↟' - Unicode \u219F).
        /// </summary>
        public readonly static char GeneralizedBy           = '↟';  // Unicode: \u219F

        internal char Relation;
        internal readonly IDependentItem Item;

        internal CycleExplainedElement( char r, IDependentItem i )
        {
            Relation = r;
            Item = i;
        }

        /// <summary>
        /// Overridden to return the "relation item (full name)".
        /// </summary>
        /// <returns>The relation.</returns>
        public override string ToString()
        {
            return String.Format( "{0} {1}", Relation, Item.FullName );
        }

        char ICycleExplainedElement.Relation { get { return Relation; } }

        IDependentItem ICycleExplainedElement.Item { get { return Item; } }
    }

}
