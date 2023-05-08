using System.Collections.Generic;
using UnityEngine;

namespace LegendaryTools.Systems
{
    public class AttributeGroupConfig<Tid, TAttr, TAttrCond, TModCond, TAttrConfig> : ScriptableObject
        where TAttr :  Attribute<Tid, TAttr, TAttrCond, TModCond, TAttrConfig>
        where TAttrCond : AttributeCondition<Tid, TModCond>
        where TModCond : AttributeModifierCondition<Tid>
        where TAttrConfig : AttributeConfig<Tid>
    {
        public List<TAttr> Attributes = new List<TAttr>();
    }
}