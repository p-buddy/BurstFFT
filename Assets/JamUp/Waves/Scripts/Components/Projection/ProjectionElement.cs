using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct ProjectionElement: IBufferElementData, IAnimatableSettable, IValueSettable<float>, IRequiredInArchetype
    {
        public float Value { get; set; }
        public AnimationCurve AnimationCurve { get; set; }
    }
}