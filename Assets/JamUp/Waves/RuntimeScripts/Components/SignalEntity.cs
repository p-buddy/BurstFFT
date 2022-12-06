using Unity.Entities;

namespace JamUp.Waves.RuntimeScripts
{
    public readonly struct SignalEntity : IComponentData, IRequiredInArchetype
    {
        public float RootFrequency { get; }

        public SignalEntity(float rootFrequency)
        {
            RootFrequency = rootFrequency;
        }
    }
}