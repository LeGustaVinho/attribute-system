#if ODIN_INSPECTOR
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace LegendaryTools.Systems.OdinInspector 
{
    public class AttributeConditionProcessor: OdinAttributeProcessor<LegendaryTools.Systems.AttributeCondition>
    {
        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<System.Attribute> attributes)
        {
            if (member.Name == nameof(AttributeCondition.ModApplicationConditions))
            {
                attributes.Add(new TableListAttribute());
            }
        }
    }
}
#endif