using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LegendaryTools.Systems
{
    public enum AttributeType
    {
        Attribute,
        Modifier
    }

    public enum AttributeModOperator
    {
        Equals,
        Greater,
        Less,
        GreaterOrEquals,
        LessOrEquals,
        NotEquals,
        ContainsFlag,
        NotContainsFlag
    }

    public enum AttributeFlagModOperator
    {
        AddFlag,
        RemoveFlag,
        Set
    }
    
    public abstract class Attribute<TID, TAttr, TAttrCond, TModCond, TAttrConfig>
        where TAttr :  Attribute<TID, TAttr, TAttrCond, TModCond, TAttrConfig>
        where TAttrCond : AttributeCondition<TID, TModCond>
        where TModCond : AttributeModifierCondition<TID>
        where TAttrConfig : AttributeConfig<TID>
    {
        [Required]
        public TAttrConfig Config;
        
        public AttributeType Type = AttributeType.Attribute;
        
        [VerticalGroup("Values")]
        public float Flat;
        [VerticalGroup("Values")][HideIf("HasOptions")]
        public float Factor = 0;

        [VerticalGroup("Values")][ShowIf("HasCapacity")]
        public float Capacity;

        [VerticalGroup("Modifiers")][ShowIf("Type", AttributeType.Modifier)]
        //List the conditions that this modifier needs to find to be applied
        public List<TAttrCond> TargetAttributeModifier = new List<TAttrCond>();
        
        [VerticalGroup("Modifiers")][ShowIf("HasOptions")]
        public AttributeFlagModOperator FlagOperator = AttributeFlagModOperator.AddFlag;

        //Lists all modifiers that are currently changing this attribute
        [ShowInInspector][VerticalGroup("Modifiers")]
        public readonly List<TAttr> Modifiers = new List<TAttr>();
        
        public IAttributeSystem<TID, TAttr, TAttrCond, TModCond,TAttrConfig> Parent { get; protected set; }

        //Returns the current value of the attribute taking into account all modifiers currently applied
        [ShowInInspector][VerticalGroup("Values")]
        public float Value => GetValueWithModifiers();

        [ShowInInspector]
        [VerticalGroup("Values")]
        [ShowIf("HasOptions")]
        public string ValueAsOption
        {
            get
            {
                if (Config == null) return string.Empty;
                int index = (int)GetValueWithModifiers();
                if (index >= Config.Options.Count) return string.Empty;
                return Config.Options[index];
            }
        }

        private bool CanUseCapacity => HasCapacity && Type == AttributeType.Attribute && !HasOptions;

        private bool HasOptions => Config?.HasOptions ?? false;

        private bool HasCapacity => Config?.HasCapacity ?? false;

        public event Action<TAttr> OnAttributeModAdd;
        public event Action<TAttr> OnAttributeModRemove;
        public event Action<float, float> OnAttributeCapacityChange;
        
        public Attribute(IAttributeSystem<TID, TAttr, TAttrCond, TModCond, TAttrConfig> parent, TAttrConfig config)
        {
            Parent = parent;
            Config = config;
        }

        public void AddModifier(TAttr attribute, TAttrCond modifier = null)
        {
            if (!ModApplicationCanBeAccepted(attribute, modifier))
            {
                return;
            }

            Modifiers.Add(attribute);

            OnAttributeModAdd?.Invoke(attribute);
        }

        public void RemoveModifier(TAttr attribute)
        {
            if (!Modifiers.Contains(attribute))
            {
                return;
            }

            Modifiers.Remove(attribute);

            OnAttributeModRemove?.Invoke(attribute);
        }

        public void RemoveModifiers(IAttributeSystem<TID, TAttr, TAttrCond, TModCond, TAttrConfig> attributeSystem)
        {
            List<TAttr> modsToRemove = Modifiers.FindAll(item => item.Parent == attributeSystem);

            Modifiers.RemoveAll(item => item.Parent == attributeSystem);

            foreach (TAttr attr in modsToRemove)
            {
                OnAttributeModRemove?.Invoke(attr);
            }
        }

        public bool CapacityAdd(float valueToAdd)
        {
            if (!CanUseCapacity)
            {
                return false;
            }

            if (!Config.AllowExceedCapacity && !(Capacity + valueToAdd <= Value))
            {
                return false;
            }

            Capacity += valueToAdd;
            OnAttributeCapacityChange?.Invoke(Capacity, Capacity - valueToAdd);

            return true;
        }

        public bool CapacityRemove(float valueToRemove)
        {
            if (!CanUseCapacity)
            {
                return false;
            }

            if (!(Capacity - valueToRemove >= Config.MinCapacity))
            {
                return false;
            }

            Capacity -= valueToRemove;
            OnAttributeCapacityChange?.Invoke(Capacity, Capacity + valueToRemove);

            return true;
        }

        /// Checks whether the mod can be applied to the target entity
        public bool ModApplicationCanBeAccepted(TAttr attribute, TAttrCond modifier = null)
        {
            if (modifier == null)
            {
                modifier = attribute.TargetAttributeModifier.Find(item => item.TargetAttributeID.Equals(Config.ID));
            }

            if (modifier != null)
            {
                TAttr currentAttribute = null;
                for (int i = 0; i < modifier.ModApplicationConditions.Count; i++)
                {
                    currentAttribute = Parent.GetAttributeByID(modifier.ModApplicationConditions[i].AttributeName);
                    switch (modifier.ModApplicationConditions[i].Operator)
                    {
                        case AttributeModOperator.Equals:
                            if (!(currentAttribute.Value == modifier.ModApplicationConditions[i].Value))
                            {
                                return false;
                            }

                            break;
                        case AttributeModOperator.Greater:
                            if (!(currentAttribute.Value > modifier.ModApplicationConditions[i].Value))
                            {
                                return false;
                            }

                            break;
                        case AttributeModOperator.Less:
                            if (!(currentAttribute.Value < modifier.ModApplicationConditions[i].Value))
                            {
                                return false;
                            }

                            break;
                        case AttributeModOperator.GreaterOrEquals:
                            if (!(currentAttribute.Value >= modifier.ModApplicationConditions[i].Value))
                            {
                                return false;
                            }

                            break;
                        case AttributeModOperator.LessOrEquals:
                            if (!(currentAttribute.Value <= modifier.ModApplicationConditions[i].Value))
                            {
                                return false;
                            }

                            break;
                        case AttributeModOperator.NotEquals:
                            if (!(currentAttribute.Value != modifier.ModApplicationConditions[i].Value))
                            {
                                return false;
                            }

                            break;
                        case AttributeModOperator.ContainsFlag:
                            if (!FlagUtil.Has(currentAttribute.Value, modifier.ModApplicationConditions[i].Value))
                            {
                                return false;
                            }

                            break;
                        case AttributeModOperator.NotContainsFlag:
                            if (FlagUtil.Has(currentAttribute.Value, modifier.ModApplicationConditions[i].Value))
                            {
                                return false;
                            }

                            break;
                    }
                }

                return true;
            }

            return false;
        }

        /// Returns the current value of the attribute taking into account all modifiers currently applied
        private float GetValueWithModifiers()
        {
            if (Config == null)
            {
                return 0;
            }

            if (HasOptions && Config.OptionsAreFlags)
            {
                float currentFlag = Flat;
                if(Modifiers != null)
                {
                    for (int i = 0; i < Modifiers.Count; i++)
                    {
                        switch (Modifiers[i].FlagOperator)
                        {
                            case AttributeFlagModOperator.AddFlag:
                                currentFlag = FlagUtil.Add(currentFlag, Modifiers[i].Flat);
                                break;
                            case AttributeFlagModOperator.RemoveFlag:
                                currentFlag = FlagUtil.Remove(currentFlag, Modifiers[i].Flat);
                                break;
                            case AttributeFlagModOperator.Set:
                                currentFlag = Modifiers[i].Flat;
                                break;
                        }
                    }
                }

                return currentFlag;
            }
            
            if (Modifiers == null)
            {
                return 0;
            }

            Modifiers.Sort((a, b) => -1 * a.Factor.CompareTo(b.Factor)); //descending sort
            float totalFlat = 0;
            float totalFactor = 0;
            for (int i = 0; i < Modifiers.Count; i++)
            {
                totalFlat += Modifiers[i].Flat;

                if (Config.HasStackPenault)
                {
                    totalFactor += Modifiers[i].Factor *
                                   Config.StackPenaults[
                                       Mathf.Clamp(i, 0, Config.StackPenaults.Length - 1)];
                }
                else
                {
                    totalFactor += Modifiers[i].Factor;
                }
            }

            return Mathf.Clamp((Flat + totalFlat) * (1 + Factor + totalFactor),
                Config.MinMaxValue.x, Config.MinMaxValue.y);
        }
    }
}