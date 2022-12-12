using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.Waves.RuntimeScripts
{
    public struct CurrentWaveAxes: IBufferElementData, IValueSettable<float3x2>, IRequiredInArchetype
    {
        [field: SerializeField]
        public float3x2 Value { get; set; }

        public float3 Start => Value.c0;
        public float3 End => Value.c1;
        
        public CurrentWaveAxes(AllWavesElement first, AllWavesElement second)
        {
            Value = AllWavesElement.PackAxes(first, second);
        }
    }
}