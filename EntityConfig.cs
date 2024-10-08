﻿using UnityEngine;

namespace LegendaryTools.Systems
{
    [CreateAssetMenu(fileName = "EntityConfig", menuName = "Tools/AttributeSystem/EntityConfig")]
    public class EntityConfig : 
#if ODIN_INSPECTOR
        Sirenix.OdinInspector.SerializedScriptableObject
#else
        ScriptableObject
#endif
        
    {
        public bool IsClone { get; private set; }
        public AttributeSystem AttributeSystem;

        private const string CLONE = "(Clone)";
        
        public virtual T Clone<T>(IAttributeSystem parent)
            where T : EntityConfig
        {
            T clone = CreateInstance<T>();
            
            clone.name = name + CLONE;
            clone.IsClone = true;
            clone.AttributeSystem = AttributeSystem.Clone(parent);
            
            return clone;
        }
    }
}