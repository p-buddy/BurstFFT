using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Entities;
using UnityEngine;

namespace JamUp.Waves.RuntimeScripts
{
    public struct ProjectionElement: IBufferElementData, IAnimatableSettable, IValueSettable<float>, IRequiredInArchetype
    {
        [field: SerializeField]
        public float Value { get; set; }
        
        [field: SerializeField]
        public AnimationCurve AnimationCurve { get; set; }
    }
}