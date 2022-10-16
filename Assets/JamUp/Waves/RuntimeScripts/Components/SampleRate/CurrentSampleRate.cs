using Unity.Entities;
using UnityEngine;

namespace JamUp.Waves.RuntimeScripts
{
    public struct CurrentSampleRate: IComponentData, IValueSettable<Animation<float>>, IRequiredInArchetype
    {
        [field: SerializeField]
        public Animation<float> Value { get; set; }
        public static implicit operator Animation<float>(CurrentSampleRate sampleRate) => sampleRate.Value;
    }
}