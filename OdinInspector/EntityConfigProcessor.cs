#if UNITY_EDITOR && ODIN_INSPECTOR
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace LegendaryTools.Systems.OdinInspector
{
    public class EntityConfigProcessor : OdinAttributeProcessor<EntityConfigProcessor>
    {
        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<System.Attribute> attributes)
        {
            if (member.Name == nameof(EntityConfig.IsClone))
            {
                attributes.Add(new ShowInInspectorAttribute());
                attributes.Add(new ReadOnlyAttribute());
            }
            
            if (member.Name == nameof(EntityConfig.AttributeSystem))
            {
                attributes.Add(new TableListAttribute());
                attributes.Add(new ShowInInspectorAttribute());
            }
        }
    }
}
#endif