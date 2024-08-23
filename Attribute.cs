using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
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
    
    [Serializable]
    public class Attribute
    {
        public AttributeConfig Config;
        public AttributeType Type = AttributeType.Attribute;
        
        public float Flat;
        public float Factor = 0;

        public float Capacity
        {
            get => capacity;
            private set => capacity = value;
        }
        
        [SerializeField]
        private float capacity;

        //List the conditions that this modifier needs to find to be applied
        public List<AttributeCondition> ModifierConditions = new List<AttributeCondition>();
        
        public AttributeFlagModOperator FlagOperator = AttributeFlagModOperator.AddFlag;

        //Lists all modifiers that are currently changing this attribute
        public List<Attribute> Modifiers = new List<Attribute>();
        
        public IAttributeSystem Parent { get; protected set; }

        public int FlatAsOptionIndex
        {
            get => (int)Flat;
            set => Flat = value;
        }
        
        public int FlatAsOptionFlag
        {
            get => (int)Flat;
            set => Flat = value;
        }
        
        //Returns the current value of the attribute taking into account all modifiers currently applied
        public float Value => GetValueWithModifiers();
        public bool ValueAsBool => Convert.ToBoolean(Value);
        public short ValueAsShort => Convert.ToInt16(Value);
        public int ValueAsInt => Convert.ToInt32(Value);
        public long ValueAsLong => Convert.ToInt64(Value);
        
        public string ValueAsOption
        {
            get
            {
                if (Config == null) return string.Empty;
                if (Config.Options == null) return string.Empty;
                int index = (int)GetValueWithModifiers();
                if (index >= Config.Options.Length || index < 0) return string.Empty;
                return Config.Options[index];
            }
        }
        
        public int ValueAsOptionFlag => (int)GetValueWithModifiers();

        public bool CanUseCapacity => HasCapacity && Type == AttributeType.Attribute && !HasOptions;

        public bool HasOptions => Config?.HasOptions ?? false;
        public bool OptionsAreFlags => Config?.OptionsAreFlags ?? false;
        public bool HasOptionsAndIsNotFlags => HasOptions && !OptionsAreFlags;
        public bool OptionsAreFlagsAndIsModifier => OptionsAreFlags && Type == AttributeType.Modifier; 
        
    #if UNITY_EDITOR && ODIN_INSPECTOR
        public IEnumerable EditorOptions
        {
            get
            {
                ValueDropdownList<int> valueDropDownList = new ValueDropdownList<int>();
                if (Config == null) return valueDropDownList;
                if (Config.Options == null) return valueDropDownList;
                for (int index = 0; index < Config.Options.Length; index++)
                {
                    valueDropDownList.Add(Config.Options[index], index);
                }

                return valueDropDownList;
            }
        }

        public string[] EditorOptionsArray
        {
            get
            {
                if (Config == null) return new string[2] {"None", "Everything"};
                if (Config.Options == null) return new string[2] {"None", "Everything"};

                return Config.Options;
            }
        }
    #endif

        public bool HasCapacity => Config?.HasCapacity ?? false;
        public bool HasParent => Parent != null;

        public event Action<Attribute> OnAttributeModAdd;
        public event Action<Attribute> OnAttributeModRemove;
        public event Action<float, float> OnAttributeCapacityChange;

#if UNITY_EDITOR && ODIN_INSPECTOR
        private int DrawFlatAsOptionFlag(int value, GUIContent label)
        {
            if (Config != null && Config.HasOptions && Config.OptionsAreFlags)
            {
                int flagResult = label == null
                    ? EditorGUILayout.MaskField(value, EditorOptionsArray)
                    : EditorGUILayout.MaskField(label, value, EditorOptionsArray);
                return flagResult == -1 ? Config.FlagOptionEverythingValue : flagResult;
            }

            return 0;
        }
        
        private int DrawValueAsOptionFlag(int value, GUIContent label)
        {
            if (Config != null && Config.HasOptions && Config.OptionsAreFlags)
            {
                GUI.enabled = false;
                if (label == null)
                    EditorGUILayout.MaskField(value == -1 ? Config.FlagOptionEverythingValue : value,
                        EditorOptionsArray);
                else    
                    EditorGUILayout.MaskField(label, value == -1 ? Config.FlagOptionEverythingValue : value, EditorOptionsArray);
                GUI.enabled = true;
            }

            return 0;
        }
#endif
        public Attribute(IAttributeSystem parent, AttributeConfig config)
        {
            Parent = parent;
            Config = config;
        }

        public bool AddModifier(Attribute modifier)
        {
            if (!ModApplicationCanBeAccepted(modifier))
            {
                return false;
            }

            Modifiers ??= new List<Attribute>();
            Modifiers.Add(modifier);
            OnAttributeModAdd?.Invoke(modifier);
            return true;
        }

        public bool RemoveModifier(Attribute attribute)
        {
            Modifiers ??= new List<Attribute>();
            if (!Modifiers.Contains(attribute))
            {
                return false;
            }

            bool removed = Modifiers.Remove(attribute);
            if(removed) OnAttributeModRemove?.Invoke(attribute);
            return removed;
        }

        public void RemoveModifiers(IAttributeSystem attributeSystem)
        {
            Modifiers ??= new List<Attribute>();
            List<Attribute> modsToRemove = Modifiers.FindAll(item => item.Parent == attributeSystem);

            Modifiers.RemoveAll(item => item.Parent == attributeSystem);

            foreach (Attribute attr in modsToRemove)
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
        public bool ModApplicationCanBeAccepted(Attribute attributeModifier)
        {
            if (attributeModifier == null)
            {
                return false;
            }

            foreach (AttributeCondition modifierCondition in attributeModifier.ModifierConditions)
            {
                if (!modifierCondition.CanBeAppliedOn(Parent))
                {
                    return false;
                }
            }

            return true;
        }

        public Attribute Clone(IAttributeSystem parent)
        {
            Attribute clone = new Attribute(parent ?? Parent, Config)
            {
                Capacity = Capacity,
                Factor = Factor,
                Flat = Flat,
                FlagOperator = FlagOperator,
                Type = Type,
            };

            foreach (AttributeCondition targetAttributeModifier in ModifierConditions)
            {
                clone.ModifierConditions.Add(targetAttributeModifier.Clone());
            }

            return clone;
        }

        /// Returns the current value of the attribute taking into account all modifiers currently applied
        private float GetValueWithModifiers()
        {
            if (Config == null)
            {
                return 0;
            }

            List<Attribute> allRecursiveModifiers = new List<Attribute>();
            GetModifiersRecursive(allRecursiveModifiers);

            if (HasOptions)
            {
                float currentFlag = Flat;

                foreach (Attribute modifier in allRecursiveModifiers)
                {
                    if (modifier.OptionsAreFlags)
                    {
                        switch (modifier.FlagOperator)
                        {
                            case AttributeFlagModOperator.AddFlag when Config.OptionsAreFlags:
                                currentFlag = FlagUtil.Add(currentFlag, modifier.Flat);
                                break;
                            case AttributeFlagModOperator.RemoveFlag when Config.OptionsAreFlags:
                                currentFlag = FlagUtil.Remove(currentFlag, modifier.Flat);
                                break;
                            case AttributeFlagModOperator.Set:
                                currentFlag = modifier.Flat;
                                break;
                        }
                    }
                    else
                    {
                        currentFlag = modifier.Flat;
                    }
                }

                return currentFlag;
            }

            allRecursiveModifiers.Sort((a, b) => -1 * a.Factor.CompareTo(b.Factor)); //descending sort
            float totalFlat = 0;
            float totalFactor = 0;
            for (int i = 0; i < allRecursiveModifiers.Count; i++)
            {
                totalFlat += allRecursiveModifiers[i].Flat;

                if (Config.HasStackPenault)
                {
                    totalFactor += allRecursiveModifiers[i].Factor *
                                   Config.StackPenaults[
                                       Mathf.Clamp(i, 0, Config.StackPenaults.Length - 1)];
                }
                else
                {
                    totalFactor += allRecursiveModifiers[i].Factor;
                }
            }

            float finalValue = (Flat + totalFlat) * (1 + Factor + totalFactor);
            return Config.HasMinMax ? Mathf.Clamp(finalValue,
                Config.MinMaxValue.x, Config.MinMaxValue.y) : finalValue;
        }

        private void GetModifiersRecursive(List<Attribute> allModifiers)
        {
            if (Modifiers.Count > 0)
            {
                allModifiers.AddRange(Modifiers);

                foreach (Attribute modifier in Modifiers)
                {
                    modifier.GetModifiersRecursive(allModifiers);
                }
            }
        }
    }
}