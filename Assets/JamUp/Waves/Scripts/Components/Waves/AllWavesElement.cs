using JamUp.Waves.Scripts.API;
using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct AllWavesElement: IBufferElementData, IValueSettable<Animatable<WaveState>>, IRequiredInArchetype
    {
        public Animatable<WaveState> Value { get; set; }
    }
}