using JamUp.Waves.RuntimeScripts.API;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.Waves.RuntimeScripts
{
    public struct CurrentWavesElement: IBufferElementData, IValueSettable<float4x4>, IRequiredInArchetype
    { 
        [field: SerializeField]
        public float4x4 Value { get; set; }
        public float3 StartFreqPhaseAmp => Value.c0.xzy;
        public AnimationCurve AnimationCurve => (AnimationCurve)(int)Value.c0.w;
        public float4 StartWaveTypeRatio => Value.c1;
        public float3 EndFreqPhaseAmp => Value.c2.xzy;
        public float4 EndWaveTypeRatio => Value.c3;

        // col 0: freq, amplitude, phase, animation
        // col 1: (sine amount, square amount, triangle amount, sawtooth amount)
        // col 2: freq, amplitude, phase, <empty>
        // col 3: (sine amount, square amount, triangle amount, sawtooth amount)
    }
}