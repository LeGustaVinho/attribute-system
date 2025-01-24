using LegendaryTools.GraphV2;
using System;
using System.Collections.Generic;
using System.Linq;
using LegendaryTools.TagSystem;
using UnityEngine;

namespace LegendaryTools.Systems
{
    [Serializable]
    public class Entity : MultiParentTreeNode, IEntity
    {
        protected EntityConfig config;
        private readonly Dictionary<AttributeConfig, Attribute> attributesLookup = new Dictionary<AttributeConfig, Attribute>();

        public virtual EntityConfig Config
        {
            get
            {
                if (config != null && !config.IsClone)
                {
                    config = config.Clone<EntityConfig>(this);
                }
                return config;
            }
            set => config = value.IsClone ? value : value.Clone<EntityConfig>(this);
        }

        public Tag[] Tags => Config.Data.tags;
        public TagFilterMatch[] OnlyAcceptTags => Config.Data.onlyAcceptTags;
        public List<Attribute> AllAttributes => Config.Data.attributes;

        public EntityManager EntityManager { private set; get; }

        public void Initialize(EntityManager entityManager)
        {
            EntityManager = entityManager;
            EntityManager.AddEntity(this);
        }
        
        public void Initialize(EntityManager entityManager, EntityConfig entityConfig)
        {
            EntityManager = entityManager;
            EntityManager.AddEntity(this);
            Config = entityConfig;
        }
        
        public void Initialize(EntityManager entityManager, EntityData entityData)
        {
            EntityManager = entityManager;
            EntityManager.AddEntity(this);
            EntityConfig prototype = ScriptableObject.CreateInstance<EntityConfig>();
            prototype.Data = entityData;
            Config = prototype;
        }

        public void Destroy()
        {
            DisconnectFromParents();
            EntityManager.RemoveEntity(this);
        }

        /// <summary>
        /// Attempts to connect this entity as a child of <paramref name="parentEntity"/>.  
        /// We also apply the modifiers from the "child" (this) to the "parent" and/or the parent's children
        /// depending on each modifier's <see cref="ModifierPropagation"/> setting.
        /// </summary>
        public (bool, INodeConnection) TryToApplyTo(Entity parentEntity)
        {
            // Checks if this entity matches all the parent's onlyAcceptTags rules
            foreach (TagFilterMatch tagFilterMatch in parentEntity.OnlyAcceptTags)
            {
                if (!tagFilterMatch.Match(this))
                    return (false, null);
            }

            INodeConnection connection = ConnectToParent(parentEntity);
            
            // Instead of calling parentEntity.AddModifiers(this) blindly,
            // we now apply this child's modifiers according to Propagation.
            parentEntity.ApplyChildModifiers(this);

            return (true, connection);
        }

        /// <summary>
        /// Called when removing the connection from *all* parents of this Entity.
        /// We remove any modifiers we had applied to those parents or their children (Child propagation).
        /// </summary>
        public override void DisconnectFromParents()
        {
            foreach (IMultiParentTreeNode parentNode in ParentNodes)
            {
                if (parentNode is Entity parentEntity)
                {
                    parentEntity.RemoveChildModifiers(this);
                }
            }

            base.DisconnectFromParents();
        }

        /// <summary>
        /// Called when removing the connection from a *single* parent of this Entity.
        /// We remove any modifiers we had applied to that parent or its children (Child propagation).
        /// </summary>
        public void DisconnectFromParent(Entity parentNode)
        {
            parentNode.RemoveChildModifiers(this);
            base.DisconnectFromParent(parentNode);
        }

        /// <summary>
        /// Returns the attribute in this Entity by its config.
        /// </summary>
        public Attribute GetAttributeByID(AttributeConfig attributeConfig, bool emitErrorIfNotFound = true)
        {
            if (attributeConfig == null)
            {
                Debug.LogError("[Entity:GetAttributeByID] attributeConfig is null.");
                return null;
            }

            if (!attributesLookup.TryGetValue(attributeConfig, out Attribute attribute))
            {
                attribute = AllAttributes.Find(item => item.Config == attributeConfig);
                if (attribute != null)
                {
                    attributesLookup.Add(attributeConfig, attribute);
                }
                else
                {
                    if(emitErrorIfNotFound)
                        Debug.LogError($"[Entity:GetAttributeByID({attributeConfig.name})] -> Not found.");
                }
            }
            return attribute;
        }

        public bool ContainsTag(Tag tag)
        {
            if (tag == null) return false;
            return Tags.Contains(tag);
        }

        /// <summary>
        /// Adds all modifier attributes from the given entity to *this* entity's matching attributes
        /// </summary>
        public void AddModifiers(IEntity entitySource)
        {
            List<Attribute> allModifiers = entitySource.AllAttributes
                .Where(item => item.Type == AttributeType.Modifier)
                .ToList();

            foreach (Attribute modifier in allModifiers)
            {
                Attribute targetAttribute = GetAttributeByID(modifier.Config, !modifier.ForceApplyIfMissing);

                if (targetAttribute == null)
                {
                    if (modifier.ForceApplyIfMissing)
                    {
                        // Cria o atributo se for para forçar aplicação
                        targetAttribute = new Attribute(this, modifier.Config)
                        {
                            Type = AttributeType.Attribute
                        };
                        AllAttributes.Add(targetAttribute);
                    }
                    else
                    {
                        // Caso contrário, não aplica
                        continue;
                    }
                }

                targetAttribute.AddModifier(modifier);
            }
        }

        /// <summary>
        /// Removes from *this* entity all modifiers that originate from the given entity.
        /// </summary>
        public void RemoveModifiers(IEntity entitySource)
        {
            foreach (Attribute attr in AllAttributes)
            {
                if (attr.Modifiers.Count > 0)
                {
                    attr.RemoveModifiers(entitySource);
                }
            }
        }

        /// <summary>
        /// Applies the "childEntity" modifiers to this entity and/or this entity's children,
        /// depending on each modifier's <see cref="ModifierPropagation"/> setting.
        /// </summary>
        public void ApplyChildModifiers(IEntity childEntity)
        {
            // Collect only the modifier attributes from the child.
            IEnumerable<Attribute> childModifiers = childEntity.AllAttributes
                .Where(a => a.Type == AttributeType.Modifier);

            foreach (Attribute mod in childModifiers)
            {
                switch (mod.Config.Propagation)
                {
                    case ModifierPropagation.Parent:
                        // Only apply to THIS (the parent).
                        AddSingleModifierToThisEntity(mod);
                        break;

                    case ModifierPropagation.Child:
                        // Only apply to THIS entity's children (not to "this" entity).
                        AddSingleModifierToChildren(mod, ChildNodes);
                        break;

                    case ModifierPropagation.Both:
                        // Apply to THIS entity and all children.
                        AddSingleModifierToThisEntity(mod);
                        AddSingleModifierToChildren(mod, ChildNodes);
                        break;
                }
            }
        }

        /// <summary>
        /// Removes modifiers that "childEntity" had previously applied to this entity
        /// and/or this entity's children, according to each modifier's propagation.
        /// </summary>
        public void RemoveChildModifiers(IEntity childEntity)
        {
            IEnumerable<Attribute> childModifiers = childEntity.AllAttributes
                .Where(a => a.Type == AttributeType.Modifier);

            foreach (Attribute mod in childModifiers)
            {
                switch (mod.Config.Propagation)
                {
                    case ModifierPropagation.Parent:
                        RemoveSingleModifierFromThisEntity(mod);
                        break;

                    case ModifierPropagation.Child:
                        RemoveSingleModifierFromChildren(mod, ChildNodes);
                        break;

                    case ModifierPropagation.Both:
                        RemoveSingleModifierFromThisEntity(mod);
                        RemoveSingleModifierFromChildren(mod, ChildNodes);
                        break;
                }
            }
        }

        /// <summary>
        /// Adds a single modifier to this entity's matching attribute.
        /// </summary>
        private void AddSingleModifierToThisEntity(Attribute mod)
        {
            // Tenta encontrar um Attribute de mesmo Config.
            Attribute targetAttribute = GetAttributeByID(mod.Config, !mod.ForceApplyIfMissing);

            // Se não existe e a flag estiver ligada, criamos dinamicamente
            if (targetAttribute == null)
            {
                if (mod.ForceApplyIfMissing)
                {
                    targetAttribute = new Attribute(this, mod.Config)
                    {
                        Type = AttributeType.Attribute // Normalmente criamos como 'Attribute', não 'Modifier'
                    };
            
                    // Adiciona na lista de atributos desta Entity
                    AllAttributes.Add(targetAttribute);
                }
                else
                {
                    // Se a flag não estiver ligada, não aplicamos nada
                    return;
                }
            }

            // Agora, com o 'targetAttribute' garantido, adicionamos o 'mod'
            targetAttribute.AddModifier(mod);
        }

        /// <summary>
        /// Removes a single modifier from this entity's matching attribute.
        /// </summary>
        private void RemoveSingleModifierFromThisEntity(Attribute mod)
        {
            Attribute targetAttribute = GetAttributeByID(mod.Config);
            targetAttribute?.RemoveModifier(mod);
        }

        /// <summary>
        /// Recursively adds a single modifier to all child entities in the tree.
        /// </summary>
        private void AddSingleModifierToChildren(Attribute mod, List<IMultiParentTreeNode> children)
        {
            Queue<IMultiParentTreeNode> queue = new Queue<IMultiParentTreeNode>(children);

            while (queue.Count > 0)
            {
                IMultiParentTreeNode node = queue.Dequeue();
                if (node is Entity childEntity)
                {
                    // Pega o atributo correspondente no filho
                    Attribute targetAttribute = childEntity.GetAttributeByID(mod.Config, !mod.ForceApplyIfMissing);

                    if (targetAttribute == null)
                    {
                        // Se a flag estiver ativa, cria dinamicamente
                        if (mod.ForceApplyIfMissing)
                        {
                            targetAttribute = new Attribute(childEntity, mod.Config)
                            {
                                Type = AttributeType.Attribute
                            };
                            childEntity.AllAttributes.Add(targetAttribute);
                        }
                        else
                        {
                            // Se não existe e a flag não está ativa, não aplicamos nada
                            continue;
                        }
                    }

                    // Agora adicionamos o modificador
                    targetAttribute.AddModifier(mod);

                    // Enfileira os filhos recursivamente
                    foreach (IMultiParentTreeNode grandchild in childEntity.ChildNodes)
                    {
                        queue.Enqueue(grandchild);
                    }
                }
            }
        }

        /// <summary>
        /// Recursively removes a single modifier from all child entities in the tree.
        /// </summary>
        private void RemoveSingleModifierFromChildren(Attribute mod, List<IMultiParentTreeNode> children)
        {
            Queue<IMultiParentTreeNode> queue = new Queue<IMultiParentTreeNode>(children);

            while (queue.Count > 0)
            {
                IMultiParentTreeNode node = queue.Dequeue();
                if (node is Entity childEntity)
                {
                    Attribute targetAttribute = childEntity.GetAttributeByID(mod.Config);
                    targetAttribute?.RemoveModifier(mod);

                    foreach (IMultiParentTreeNode grandchild in childEntity.ChildNodes)
                    {
                        queue.Enqueue(grandchild);
                    }
                }
            }
        }
    }
}