using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        public float Factor;

        public float CurrentValue
        {
            get => currentValue;
            private set => currentValue = value;
        }

        [SerializeField] [HideInInspector] private float currentValue;

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
                if (Config == null) return new string[2] { "None", "Everything" };
                if (Config.Options == null) return new string[2] { "None", "Everything" };

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
                {
                    EditorGUILayout.MaskField(value == -1 ? Config.FlagOptionEverythingValue : value,
                        EditorOptionsArray);
                }
                else
                {
                    EditorGUILayout.MaskField(label, value == -1 ? Config.FlagOptionEverythingValue : value,
                        EditorOptionsArray);
                }

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
            if (removed) OnAttributeModRemove?.Invoke(attribute);
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
        public bool AddUsage(float valueToAdd)
        {
            if (!CanUseCapacity)
            {
                Debug.LogError("Cannot use capacity: either capacity is disabled or the attribute type does not support it.");
                return false;
            }

            if (valueToAdd < 0)
            {
                Debug.LogError("Cannot add a negative value to capacity.");
                return false;
            }

            float newCapacity = CurrentValue + valueToAdd;

            if (!Config.AllowExceedCapacity && newCapacity > Value)
            {
                Debug.LogWarning("Addition exceeds the maximum capacity. Clamping to maximum.");
                newCapacity = Value;
            }

            if (newCapacity < Config.MinCapacity)
            {
                Debug.LogError("New capacity is below the minimum allowed capacity.");
                return false;
            }

            float previousCapacity = CurrentValue;
            CurrentValue = newCapacity;
            OnAttributeCapacityChange?.Invoke(CurrentValue, previousCapacity);

            return true;
        }

        public bool RemoveUsage(float valueToRemove)
        {
            if (!CanUseCapacity)
            {
                Debug.LogError("Cannot use capacity: either capacity is disabled or the attribute type does not support it.");
                return false;
            }

            if (valueToRemove < 0)
            {
                Debug.LogError("Cannot remove a negative value from capacity.");
                return false;
            }

            float newCapacity = CurrentValue - valueToRemove;

            if (newCapacity < Config.MinCapacity)
            {
                Debug.LogWarning("Removal causes capacity to fall below the minimum. Clamping to minimum.");
                newCapacity = Config.MinCapacity;
            }

            float previousCapacity = CurrentValue;
            CurrentValue = newCapacity;
            OnAttributeCapacityChange?.Invoke(CurrentValue, previousCapacity);

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
                CurrentValue = CurrentValue,
                Factor = Factor,
                Flat = Flat,
                FlagOperator = FlagOperator,
                Type = Type
            };

            foreach (AttributeCondition targetAttributeModifier in ModifierConditions)
            {
                clone.ModifierConditions.Add(targetAttributeModifier.Clone());
            }

            return clone;
        }

        /// <summary>
        ///     Calculates the current value of the attribute, incorporating all modifiers and configuration rules.
        /// </summary>
        /// <remarks>
        ///     The method accounts for flat and factor values of modifiers, options, flag operations, and configuration
        ///     constraints.
        ///     - **Modifiers:** Applied recursively and sorted by their factor values in descending order.
        ///     - **Flag Operations:** If the attribute uses flags (bitwise operations), modifiers are processed accordingly.
        ///     - **Stack Penalties:** If the configuration defines penalties for stacking modifiers, these are applied per
        ///     modifier level.
        ///     - **Min-Max Clamping:** Ensures the result respects the min and max limits defined in the configuration.
        /// </remarks>
        /// <returns>The calculated attribute value after applying all modifiers.</returns>
        private float GetValueWithModifiers()
        {
            if (Config == null)
            {
                // If no configuration is provided, return the default value of 0.
                return 0;
            }

            // Collect all modifiers recursively from the attribute and its children modifiers, this is necessary because a modifier can have another modifier
            List<Attribute> allRecursiveModifiers = new List<Attribute>();
            GetModifiersRecursive(allRecursiveModifiers);

            if (HasOptions)
            {
                // Handle flag-based attributes using bitwise operations.
                float currentFlag = Flat;

                foreach (Attribute modifier in allRecursiveModifiers)
                {
                    if (modifier.OptionsAreFlags)
                    {
                        // Apply bitwise operations based on the modifier's flag operator.
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
                        // For non-flag options, override the current flag value with the modifier's flat value.
                        currentFlag = modifier.Flat;
                    }
                }

                return currentFlag;
            }

            // Sort modifiers by factor in descending order because we want to apply modifier with high values first
            allRecursiveModifiers.Sort((a, b) => -1 * a.Factor.CompareTo(b.Factor));

            // Calculate the total flat and factor values from all modifiers.
            float totalFlat = 0;
            float totalFactor = 0;

            for (int i = 0; i < allRecursiveModifiers.Count; i++)
            {
                totalFlat += allRecursiveModifiers[i].Flat;

                if (Config.HasStackPenault)
                {
                    // Apply stack penalties to the factor value.
                    totalFactor += allRecursiveModifiers[i].Factor *
                                   Config.StackPenaults[
                                       Mathf.Clamp(i, 0, Config.StackPenaults.Length - 1)];
                }
                else
                {
                    totalFactor += allRecursiveModifiers[i].Factor;
                }
            }

            // Compute the final value
            float finalValue = (Flat + totalFlat) * (1 + Factor + totalFactor);

            // Clamp the result within the min and max range if configured.
            return Config.HasMinMax ? Mathf.Clamp(finalValue, Config.MinMaxValue.x, Config.MinMaxValue.y) : finalValue;
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