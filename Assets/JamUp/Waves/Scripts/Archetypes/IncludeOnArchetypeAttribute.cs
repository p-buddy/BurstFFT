using System;

namespace JamUp.Waves.Scripts.Archetypes
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
    public class IncludeOnArchetypeAttribute: Attribute
    {
        public Type Type { get; }
        
        public IncludeOnArchetypeAttribute(Type type)
        {
            Type = type;
        }
    }
}