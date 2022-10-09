using JamUp.UnityUtility;
using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct DurationElement: IBufferElementData, IValueSettable<float>, IRequiredInArchetype
    {
        public float Value { get; set; }
        
        public static implicit operator float(DurationElement element) => element.Value;

        public DurationElement(float duration)
        {
            Value = duration;
        }
    }
}