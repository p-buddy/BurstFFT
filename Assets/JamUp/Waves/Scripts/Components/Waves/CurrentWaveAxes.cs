using Unity.Entities;
using Unity.Mathematics;

namespace JamUp.Waves.Scripts
{
    public struct CurrentWaveAxes: IBufferElementData, IValueSettable<float3x2>, IRequiredInArchetype
    {
        public float3x2 Value { get; set; }

        public float3 Start => Value.c0;
        public float3 End => Value.c1;
    }
}