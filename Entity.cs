using System;
using System.Collections.Generic;
using LegendaryTools.GraphV2;
using LegendaryTools.TagSystem;

namespace LegendaryTools.Systems
{
    [Serializable]
    public class Entity : MultiParentTreeNode, IAttributeSystem
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

        
        public Tag[] Tags => Config.AttributeSystem.Tags;
        public TagFilterMatch[] OnlyAcceptTags => Config.AttributeSystem.OnlyAcceptTags;
        public List<Attribute> AllAttributes => Config?.AttributeSystem.Attributes;
        
        public EntityManager EntityManager { private set; get; }

        public Entity(EntityManager entityManager)
        {
            EntityManager = entityManager;
            EntityManager.AddEntity(this);
        }

        public void Destroy()
        {
            DisconnectFromParents();
            EntityManager.RemoveEntity(this);
        }

        public (bool, INodeConnection) TryToApplyTo(Entity parentEntity)
        {
            foreach (TagFilterMatch tagFilterMatch in parentEntity.Config.AttributeSystem.OnlyAcceptTags)
            {
                if(!tagFilterMatch.Match(Config.AttributeSystem)) return (false, null);
            }
            INodeConnection connection = ConnectToParent(parentEntity);
            parentEntity.AddModifiers(this);
            return (true, connection);
        }

        public override void DisconnectFromParents()
        {
            foreach (IMultiParentTreeNode parentNode in ParentNodes)
            {
                if(parentNode is Entity parentEntity)
                {
                    parentEntity.RemoveModifiers(this);
                }
            }

            base.DisconnectFromParents();
        }

        public void DisconnectFromParent(Entity parentNode)
        {
            parentNode.RemoveModifiers(this);
            base.DisconnectFromParent(parentNode);
        }

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
        
        public bool ContainsTag(Tag tag)
        {
            return Config.AttributeSystem.ContainsTag(tag);
        }
    }
}