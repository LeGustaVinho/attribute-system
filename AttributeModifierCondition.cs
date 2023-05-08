using System;

namespace LegendaryTools.Systems
{
    [Serializable]
    public class AttributeModifierCondition<Tid>
    {
        public Tid AttributeName;
        public AttributeModOperator Operator;
        public float Value;
    }
}