using Unity.Entities;

namespace JamUp.Waves.RuntimeScripts.Audio
{
    public readonly struct AudioGraphReference: IComponentData, IRequiredInArchetype
    {
        public int Index { get; }
        
        public AudioGraphReference(int index)
        {
            Index = index;
        }
    }
}