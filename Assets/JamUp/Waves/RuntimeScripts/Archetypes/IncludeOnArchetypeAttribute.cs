using System;

namespace JamUp.Waves.RuntimeScripts.Archetypes
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