using JamUp.Waves.RuntimeScripts.API;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.Waves.RuntimeScripts
{
    public struct AllWavesElement: IBufferElementData, IRequiredInArchetype
    {
        public float Frequency;
        public float Amplitude;
        public float4 WaveTypeRatio;
        public float Phase;
        public float3 DisplacementAxis;
        public AnimationCurve AnimationCurve;

        public static float4x4 PackSettings(AllWavesElement first, AllWavesElement second, float m33 = default) 
            => new (new float4(first.Frequency, first.Amplitude, first.Phase, (int)first.AnimationCurve), 
                    first.WaveTypeRatio,
                    new float4(second.Frequency, second.Amplitude, second.Phase, m33),
                    second.WaveTypeRatio);

        public static float3x2 PackAxes(AllWavesElement first, AllWavesElement second)
            => new (first.DisplacementAxis, second.DisplacementAxis);

        public AllWavesElement Default
        {
            get
            {
                AnimationCurve = AnimationCurve.Linear;
                Amplitude = 0f;
                return this;
            }
        }

        public static AllWavesElement FromLerp(float lerpTime, CurrentWavesElement wave, CurrentWaveAxes axis, AnimationCurve curve = default)
        {
            float s = wave.AnimationCurve.GetLerpParameter(lerpTime);
            float3 freqPhaseAmp = math.lerp(wave.StartFreqPhaseAmp, wave.EndFreqPhaseAmp, s);
            return new()
            {
                Frequency = freqPhaseAmp.x,
                Phase = freqPhaseAmp.y,
                Amplitude = freqPhaseAmp.z,
                AnimationCurve = curve,
                DisplacementAxis = math.lerp(axis.Start, axis.End, s),
                WaveTypeRatio = math.lerp(wave.StartWaveTypeRatio, wave.EndWaveTypeRatio, s),
            };
        }
    }
}