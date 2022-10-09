using Unity.Entities;
using Unity.Mathematics;

namespace JamUp.Waves.Scripts
{
    public struct CurrentWavesElement: IBufferElementData, IValueSettable<float4x4>, IRequiredInArchetype
    { 
        public  float4x4 Value { get; set; }
    }
}