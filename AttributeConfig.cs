using System.Collections.Generic;
using UnityEngine;

namespace LegendaryTools.Systems
{
    public class AttributeConfig<T> : ScriptableObject
    {
        public T ID;

        public bool OptionsAreFlags;
        public List<string> Options = new List<string>();

        public bool HasCapacity;
        public bool AllowExceedCapacity;
        public float MinCapacity;

        public Vector2 MinMaxValue;

        public float[] StackPenaults;

        public bool HasOptions => Options.Count > 0;

        public int FlagOptionEverythingValue => (int) Mathf.Pow(2, Options.Count) - 1;

        public bool HasStackPenault => StackPenaults != null && StackPenaults.Length > 0;
    }
}