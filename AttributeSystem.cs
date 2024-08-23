using System;
using System.Collections.Generic;
using UnityEngine;

namespace LegendaryTools.Systems
{
    public interface IAttributeSystem
    {
        List<Attribute> AllAttributes { get; }
        void AddModifiers(IAttributeSystem attributeSystem);
        void RemoveModifiers(IAttributeSystem attributeSystem);
        Attribute GetAttributeByID(AttributeConfig attributeConfig);
        AttributeSystem Clone(IAttributeSystem newParent);
    }

    [Serializable]
    public class AttributeSystem : IAttributeSystem
    {
        public List<Attribute> Attributes = new List<Attribute>();
        private readonly Dictionary<AttributeConfig, Attribute> attributesLookup = new Dictionary<AttributeConfig, Attribute>();

        public List<Attribute> AllAttributes => Attributes;

        public void AddModifiers(IAttributeSystem attributeSystem)
        {
            List<Attribute> allModifiers =
                attributeSystem.AllAttributes.FindAll(item => item.Type == AttributeType.Modifier);

            foreach (Attribute modifier in allModifiers)
            {
                Attribute targetAttribute = GetAttributeByID(modifier.Config);
                targetAttribute?.AddModifier(modifier);
            }
        }
    
        public void RemoveModifiers(IAttributeSystem attributeSystem)
        {
            foreach (Attribute attr in Attributes)
            {
                if (attr.Modifiers.Count > 0)
                {
                    attr.RemoveModifiers(attributeSystem);
                }
            }
        }
    
        public Attribute GetAttributeByID(AttributeConfig attributeConfig)
        {
            if (attributesLookup.ContainsKey(attributeConfig))
            {
                return attributesLookup[attributeConfig];
            }

            Attribute attr = Attributes.Find(item => item.Config == attributeConfig);
            if (attr != null)
            {
                attributesLookup.Add(attributeConfig, attr);
                return attr;
            }

            Debug.LogError($"[AttributeSystem:GetAttributeByID({attributeConfig.name}) -> Not found");
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