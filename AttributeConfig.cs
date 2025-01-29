using UnityEngine;

namespace LegendaryTools.Systems
{
    [CreateAssetMenu(fileName = "New AttributeConfig", menuName = "Tools/AttributeSystem/AttributeConfig")]
    public class AttributeConfig : ScriptableObject
    {
        public AttributeData Data;
    }
}