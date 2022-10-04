using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct CurrentSignalLength: IComponentData, IValueSettable<Animation<float>>
    {
        public Animation<float> Value { get; set; }
        public static implicit operator Animation<float>(CurrentSignalLength signalLength) => signalLength.Value;
    }
    
    public readonly struct SignalLengthElement: IBufferElementData, IAnimatable, IValuable<float>
    {
        public float Value { get; }
        public AnimationCurve AnimationCurve { get; }

        public SignalLengthElement(float signalLength, AnimationCurve curve)
        {
            Value = signalLength;
            AnimationCurve = curve;
        }
    }
}