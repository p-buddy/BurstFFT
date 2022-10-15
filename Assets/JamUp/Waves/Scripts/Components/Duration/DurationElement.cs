using Unity.Entities;
using UnityEngine;

namespace JamUp.Waves.Scripts
{
    public struct DurationElement: IBufferElementData, IValueSettable<float>, IRequiredInArchetype
    {
        [field: SerializeField]
        public float Value { get; set; }
        
        public static implicit operator float(DurationElement element) => element.Value;

        public DurationElement(float duration)
        {
            Value = duration;
        }
    }
}