#if UNITY_EDITOR && ODIN_INSPECTOR
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace LegendaryTools.Systems.OdinInspector
{
    public class EntityProcessor : OdinAttributeProcessor<EntityProcessor>
    {
        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<System.Attribute> attributes)
        {
            if (member.Name == nameof(Entity.Config))
            {
                attributes.Add(new FoldoutGroupAttribute(groupName: "Entity"));
                attributes.Add(new ShowInInspectorAttribute());
                attributes.Add(new InlineEditorAttribute());
            }
        }
    }
}
#endif