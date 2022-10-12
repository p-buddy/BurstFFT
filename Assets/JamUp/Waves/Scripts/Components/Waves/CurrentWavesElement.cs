using JamUp.Waves.Scripts.API;
using Unity.Entities;
using Unity.Mathematics;

namespace JamUp.Waves.Scripts
{
    public struct CurrentWavesElement: IBufferElementData, IValueSettable<float4x4>, IRequiredInArchetype
    { 
        public float4x4 Value { get; set; }
        public float3 StartFreqPhaseAmp => Value.c0.xyz;
        public AnimationCurve AnimationCurve => (AnimationCurve)(int)Value.c0.w;
        public float4 StartWaveTypeRatio => Value.c1;
        public float3 EndFreqPhaseAmp => Value.c2.xyz;
        public float4 EndWaveTypeRatio => Value.c3;

        // col 0: freq, phase, amplitude, animation
        // col 1: (sine amount, square amount, triangle amount, sawtooth amount)
        // col 2: freq, phase, amplitude, <empty>
        // col 3: (sine amount, square amount, triangle amount, sawtooth amount)
    }
}