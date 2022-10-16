using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Entities;
using UnityEngine;

namespace JamUp.Waves.RuntimeScripts
{
    public struct ThicknessElement: IBufferElementData, IAnimatableSettable, IValueSettable<float>, IRequiredInArchetype
    {
        [field: SerializeField]
        public float Value { get; set; }
        public AnimationCurve AnimationCurve { get; set; }
    }
}