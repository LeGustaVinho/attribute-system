using System;
using System.Collections.Generic;
using UnityEngine;

namespace LegendaryTools.Systems
{
    public interface IAttributeSystem
    {
        void AddModifiers(AttributeSystem attributeSystem);
        void RemoveModifiers(AttributeSystem attributeSystem);
        Attribute GetAttributeByID(AttributeConfig attributeName);
    }

    [Serializable]
    public class AttributeSystem : IAttributeSystem
    {
        public List<Attribute> Attributes = new List<Attribute>();
        private readonly Dictionary<AttributeConfig, Attribute> attributesLookup = new Dictionary<AttributeConfig, Attribute>();
    
        public void AddModifiers(AttributeSystem attributeSystem)
        {
            List<Attribute> allModifiers =
                attributeSystem.Attributes.FindAll(item => item.Type == AttributeType.Modifier);

            foreach (Attribute attrMod in allModifiers)
            {
                foreach (AttributeCondition targetAttrMod in attrMod.TargetAttributeModifier)
                {
                    Attribute currentAttribute = GetAttributeByID(targetAttrMod.TargetAttributeID);
                    currentAttribute?.AddModifier(attrMod, targetAttrMod);
                }
            }
        }
    
        public void RemoveModifiers(AttributeSystem attributeSystem)
        {
            foreach (Attribute attr in Attributes)
            {
                if (attr.Modifiers.Count > 0)
                {
                    attr.RemoveModifiers(attributeSystem);
                }
            }
        }
    
        public Attribute GetAttributeByID(AttributeConfig attributeName)
        {
            if (attributesLookup.ContainsKey(attributeName))
            {
                return attributesLookup[attributeName];
            }

            Attribute attr = Attributes.Find(item => item.Config == attributeName);
            if (attr != null)
            {
                attributesLookup.Add(attributeName, attr);
                return attr;
            }

            Debug.LogError("[AttributeSystem:GetAttributeByID(" + attributeName + ") -> Not found");
            return null;
        }

        public AttributeSystem Clone(IAttributeSystem newParent)
        {
            AttributeSystem clone = new AttributeSystem();
            foreach (Attribute attribute in Attributes)
            {
                clone.Attributes.Add(attribute.Clone(newParent ?? this));
            }
            
            return clone;
        }
    }
}