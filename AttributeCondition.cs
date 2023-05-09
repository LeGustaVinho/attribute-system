using System;
using System.Collections.Generic;

namespace LegendaryTools.Systems
{
    [Serializable]
    public class AttributeCondition
    {
        /// Designates which attribute this modifier will change
        public AttributeConfig TargetAttributeID;
        
        /// Lists all rules that must be met for the modifier to be applied to the attribute of the target entity
        public List<AttributeModifierCondition> ModApplicationConditions = new List<AttributeModifierCondition>();

        public AttributeCondition Clone()
        {
            AttributeCondition clone = new AttributeCondition
            {
                TargetAttributeID = TargetAttributeID
            };
            foreach (AttributeModifierCondition modApplicationCondition in ModApplicationConditions)
            {
                clone.ModApplicationConditions.Add(modApplicationCondition.Clone());
            }
            return clone;
        }
    }
}