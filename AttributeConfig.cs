using System.Collections.Generic;
using UnityEngine;

namespace LegendaryTools.Systems
{
    [CreateAssetMenu(fileName = "New AttributeConfig", menuName = "Tools/AttributeSystem/AttributeConfig")]
    public class AttributeConfig : ScriptableObject
    {
        public bool OptionsAreFlags;
        public string[] Options;

        public bool HasCapacity;
        public bool AllowExceedCapacity;
        public float MinCapacity;

        public bool HasMinMax;
        public Vector2 MinMaxValue;

        public float[] StackPenaults;
        
        /// <summary>
        /// Configures how modifiers propagate when this attribute is used as a modifier.
        /// </summary>
        public ModifierPropagation Propagation = ModifierPropagation.Parent;

        public bool HasOptions => Options != null && Options.Length > 0;

        public int FlagOptionEverythingValue => Options != null ? (int) Mathf.Pow(2, Options.Length) - 1 : 0;

        public bool HasStackPenault => StackPenaults != null && StackPenaults.Length > 0;
    }
}