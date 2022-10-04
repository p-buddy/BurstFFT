using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct CurrentProjection: IComponentData, IValueSettable<Animation<float>>
    {
        public Animation<float> Value { get; set; }
        public static implicit operator Animation<float>(CurrentProjection projection) => projection.Value;
    }
    
    public readonly struct ProjectionElement: IBufferElementData, IAnimatable, IValuable<float>
    {
        public float Value { get; }
        public AnimationCurve AnimationCurve { get; }

        public ProjectionElement(ProjectionType projectionType, AnimationCurve curve)
        {
            Value = (int)projectionType;
            AnimationCurve = curve;
        }
    }
}