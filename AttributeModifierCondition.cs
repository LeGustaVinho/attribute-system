using System;

namespace LegendaryTools.Systems
{
    [Serializable]
    public class AttributeModifierCondition
    {
        public AttributeConfig AttributeName;
        public AttributeModOperator Operator;
        public float Value;

        public AttributeModifierCondition Clone()
        {
            AttributeModifierCondition clone = new AttributeModifierCondition
            {
                AttributeName = AttributeName,
                Operator = Operator,
                Value = Value
            };
            return clone;
        }
    }
}