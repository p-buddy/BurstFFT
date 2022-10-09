using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct CurrentWaveCount : IComponentData, IValueSettable<int>, IRequiredInArchetype
    {
        public int Value { get; set; }
    }
}