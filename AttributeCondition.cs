using System;
using System.Collections.Generic;

namespace LegendaryTools.Systems
{
    [Serializable]
    public class AttributeCondition<Tid, TModCond>
        where TModCond : AttributeModifierCondition<Tid>
    {
        /// Lists all rules that must be met for the modifier to be applied to the attribute of the target entity
        public List<TModCond> ModApplicationConditions = new List<TModCond>();

        /// Designates which attribute this modifier will change
        public Tid TargetAttributeID;
    }
}