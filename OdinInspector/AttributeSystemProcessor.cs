#if ODIN_INSPECTOR
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace LegendaryTools.Systems.OdinInspector
{
    public class AttributeSystemProcessor : OdinAttributeProcessor<AttributeSystem>
    {
        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<System.Attribute> attributes)
        {
            if (member.Name == nameof(AttributeSystem.Attributes))
            {
                attributes.Add(new TableListAttribute());
            }
        }

        public override void ProcessSelfAttributes(InspectorProperty property, List<System.Attribute> attributes)
        {
            attributes.Add(new HideLabelAttribute());
        }
    }
}
#endif