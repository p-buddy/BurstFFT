using JamUp.UnityUtility;
using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct CurrentTimeFrame: IComponentData
    {
        public float StartTime;
        public float EndTime;

        public bool UpdateRequired(float time, float padding) => time + padding >= EndTime;
    }
    
    public readonly struct DurationElement: IBufferElementData
    {
        public float Value { get; }
        
        public static implicit operator float(DurationElement element) => element.Value;

        public DurationElement(float duration)
        {
            Value = duration;
        }
    }
}