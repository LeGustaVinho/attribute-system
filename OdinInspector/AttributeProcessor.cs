#if ODIN_INSPECTOR
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace LegendaryTools.Systems.OdinInspector
{
    public class AttributeProcessor : OdinAttributeProcessor<LegendaryTools.Systems.Attribute>
    {
        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<System.Attribute> attributes)
        {
            if (member.Name == nameof(Attribute.Config))
            {
                attributes.Add(new InlineEditorAttribute());
            }

            if (member.Name == nameof(Attribute.Type) ||
                member.Name == nameof(Attribute.Flat) ||
                member.Name == nameof(Attribute.Factor) ||
                member.Name == nameof(Attribute.Capacity) ||
                member.Name == nameof(Attribute.Value) ||
                member.Name == nameof(Attribute.ValueAsOption) ||
                member.Name == nameof(Attribute.FlatAsOptionIndex) ||
                member.Name == nameof(Attribute.FlatAsOptionFlag) ||
                member.Name == nameof(Attribute.ValueAsOptionFlag)
                )
            {
                attributes.Add(new VerticalGroupAttribute("Value"));
            }

            if (member.Name == nameof(Attribute.FlatAsOptionIndex))
            {
                attributes.Add(new ShowInInspectorAttribute());
                attributes.Add(new ValueDropdownAttribute(nameof(Attribute.EditorOptions)));
                attributes.Add(new ShowIfAttribute(nameof(Attribute.HasOptions)));
                attributes.Add(new HideIfAttribute(nameof(Attribute.OptionsAreFlags)));
            }
            
            if (member.Name == nameof(Attribute.FlatAsOptionFlag))
            {
                attributes.Add(new ShowInInspectorAttribute());
                attributes.Add(new CustomValueDrawerAttribute("DrawFlatAsOptionFlag"));
                attributes.Add(new ShowIfAttribute(nameof(Attribute.OptionsAreFlags)));
            }
            
            if (member.Name == nameof(Attribute.Value))
            {
                attributes.Add(new ShowInInspectorAttribute());
            }
            
            if (member.Name == nameof(Attribute.ValueAsOption))
            {
                attributes.Add(new ShowInInspectorAttribute());
                attributes.Add(new ShowIfAttribute(nameof(Attribute.HasOptions)));
                attributes.Add(new HideIfAttribute(nameof(Attribute.OptionsAreFlags)));
            }
            
            if (member.Name == nameof(Attribute.ValueAsOptionFlag))
            {
                attributes.Add(new ShowInInspectorAttribute());
                attributes.Add(new CustomValueDrawerAttribute("DrawFlatAsOptionFlag"));
                attributes.Add(new ShowIfAttribute(nameof(Attribute.OptionsAreFlags)));
            }
            
            if (member.Name == nameof(Attribute.Capacity))
            {
                attributes.Add(new ShowIfAttribute(nameof(Attribute.HasCapacity)));
            }
            
            if (member.Name == nameof(Attribute.TargetAttributeModifier) ||
                member.Name == nameof(Attribute.FlagOperator))
            {
                attributes.Add(new VerticalGroupAttribute("Mods"));
                attributes.Add(new ShowIfAttribute(nameof(Attribute.Type), AttributeType.Modifier));
            }

            if (member.Name == nameof(Attribute.FlagOperator))
            {
                attributes.Add(new ShowIfAttribute(nameof(Attribute.OptionsAreFlags)));
            }
            
            if (member.Name == nameof(Attribute.TargetAttributeModifier))
            {
                attributes.Add(new TableListAttribute());
            }
        }
    }
}
#endif