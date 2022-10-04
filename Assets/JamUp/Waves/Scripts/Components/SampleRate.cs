using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct CurrentSampleRate: IComponentData, IValueSettable<Animation<float>>
    {
        public Animation<float> Value { get; set; }
        public static implicit operator Animation<float>(CurrentSampleRate sampleRate) => sampleRate.Value;
    }
    
    public readonly struct SampleRateElement: IBufferElementData, IAnimatable, IValuable<float>
    {
        public float Value { get; }
        public AnimationCurve AnimationCurve { get; }

        public SampleRateElement(int sampleRate, AnimationCurve curve)
        {
            Value = sampleRate;
            AnimationCurve = curve;
        }
    }
}