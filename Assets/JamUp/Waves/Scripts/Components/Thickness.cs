using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct CurrentThickness: IComponentData, IValueSettable<Animation<float>>
    {
        public Animation<float> Value { get; set; }
        public static implicit operator Animation<float>(CurrentThickness projection) => projection.Value;
    }
    
    public readonly struct ThicknessElement: IBufferElementData, IAnimatable, IValuable<float>
    {
        public float Value { get; }
        public AnimationCurve AnimationCurve { get; }

        public ThicknessElement(float thickness, AnimationCurve curve)
        {
            Value = thickness;
            AnimationCurve = curve;
        }
    }
}