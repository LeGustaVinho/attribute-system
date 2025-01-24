using System.Collections.Generic;
using LegendaryTools.TagSystem;

namespace LegendaryTools.Systems
{
    public interface IEntity : ITaggable
    {
        public TagFilterMatch[] OnlyAcceptTags { get; }
        
        List<Attribute> AllAttributes { get; }
        void AddModifiers(IEntity entitySource);
        void RemoveModifiers(IEntity entitySource);
        Attribute GetAttributeByID(AttributeConfig attributeConfig, bool emitErrorIfNotFound = true);
    }
}