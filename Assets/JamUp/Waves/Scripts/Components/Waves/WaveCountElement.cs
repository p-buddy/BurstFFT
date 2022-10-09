using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct WaveCountElement: IBufferElementData, IValueSettable<int>, IRequiredInArchetype
    {
        public int Value { get; set; }
    }
}