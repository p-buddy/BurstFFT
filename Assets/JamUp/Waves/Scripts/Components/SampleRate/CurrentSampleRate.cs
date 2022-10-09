using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct CurrentSampleRate: IComponentData, IValueSettable<Animation<float>>, IRequiredInArchetype
    {
        public Animation<float> Value { get; set; }
        public static implicit operator Animation<float>(CurrentSampleRate sampleRate) => sampleRate.Value;
    }
}