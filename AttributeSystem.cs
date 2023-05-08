using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LegendaryTools.Systems
{
    public interface IAttributeSystem<Tid, TAttr, TAttrCond, TModCond, TAttrConfig>
        where TAttr :  Attribute<Tid, TAttr, TAttrCond, TModCond, TAttrConfig>
        where TAttrCond : AttributeCondition<Tid, TModCond>
        where TModCond : AttributeModifierCondition<Tid>
        where TAttrConfig : AttributeConfig<Tid>
    {
        List<TAttr> AttributesList { get; }
        
        public TAttr GetAttributeByID(Tid attributeName);
    }
    
    public class AttributeSystem
    {
        public static void AddModifiers<Tid, TAttr, TAttrCond, TModCond, TAttrConfig>(
            IAttributeSystem<Tid, TAttr, TAttrCond, TModCond, TAttrConfig> target,
            IAttributeSystem<Tid, TAttr, TAttrCond, TModCond, TAttrConfig> source)
            where TAttr :  Attribute<Tid, TAttr, TAttrCond, TModCond, TAttrConfig>
            where TAttrCond : AttributeCondition<Tid, TModCond>
            where TModCond : AttributeModifierCondition<Tid>
            where TAttrConfig : AttributeConfig<Tid>
        {
            List<TAttr> allModifiers =
                source.AttributesList.FindAll(item => item.Type == AttributeType.Modifier);

            TAttr currentAttribute = null;
            for (int i = 0; i < allModifiers.Count; i++)
            {
                for (int j = 0; j < allModifiers[i].TargetAttributeModifier.Count; j++)
                {
                    currentAttribute = target.GetAttributeByID(allModifiers[i].TargetAttributeModifier[j].TargetAttributeID);
                    if (currentAttribute != null)
                    {
                        currentAttribute.AddModifier(allModifiers[i], allModifiers[i].TargetAttributeModifier[j]);
                    }
                }
            }
        }

        public static void RemoveModifiers<Tid, TAttr, TAttrCond, TModCond, TAttrConfig>(
            IAttributeSystem<Tid, TAttr, TAttrCond, TModCond, TAttrConfig> target,
            IAttributeSystem<Tid, TAttr, TAttrCond, TModCond, TAttrConfig> source)
            where TAttr :  Attribute<Tid, TAttr, TAttrCond, TModCond, TAttrConfig>
            where TAttrCond : AttributeCondition<Tid, TModCond>
            where TModCond : AttributeModifierCondition<Tid>
            where TAttrConfig : AttributeConfig<Tid>
        {
            for (int i = 0; i < target.AttributesList.Count; i++)
            {
                if (target.AttributesList[i].Modifiers.Count > 0)
                {
                    target.AttributesList[i].RemoveModifiers(source);
                }
            }
        }
    }
}