using System.ComponentModel;

namespace JamUp.Waves.Scripts.Archetypes
{
    [IncludeOnArchetype(typeof(Projection))]
    [IncludeOnArchetype(typeof(WaveBufferElement))]
    [IncludeOnArchetype(typeof(WaveDetails))]
    public struct Transition: IEntityArchetype
    {
        
    }
}