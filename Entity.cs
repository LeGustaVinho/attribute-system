using System;
using System.Collections.Generic;

namespace LegendaryTools.Systems
{
    [Serializable]
    public class Entity : IAttributeSystem
    {
        public virtual EntityConfig Config
        {
            get
            {
                if (config != null)
                {
                    if(!config.IsClone)
                        config = config.Clone<EntityConfig>(this);
                }

                return config;
            }
            set => config = value.IsClone ? value : value.Clone<EntityConfig>(this);
        }

        protected EntityConfig config;

        public List<Attribute> AllAttributes => Config?.AttributeSystem.Attributes;

        public void AddModifiers(IAttributeSystem attributeSystem)
        {
            if(Config == null) return;
            Config.AttributeSystem.AddModifiers(attributeSystem);
        }

        public void RemoveModifiers(IAttributeSystem attributeSystem)
        {
            if(Config == null) return;
            Config.AttributeSystem.RemoveModifiers(attributeSystem);
        }

        public Attribute GetAttributeByID(AttributeConfig attributeConfig)
        {
            if(Config == null) return null;
            return Config.AttributeSystem.GetAttributeByID(attributeConfig);
        }

        public virtual AttributeSystem Clone(IAttributeSystem newParent)
        {
            if(Config == null) return null;
            return Config.AttributeSystem.Clone(newParent);
        }
    }
}