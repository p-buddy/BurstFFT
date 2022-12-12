using JamUp.Waves.RuntimeScripts.API;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.Waves.RuntimeScripts
{
    [BurstCompile]
    public struct CurrentWavesElement : IBufferElementData, IValueSettable<float4x4>, IRequiredInArchetype
    {
        private const float TwoOverPI = 2 / math.PI;
        private const float PIOverTwo = math.PI / 2;

        public CurrentWavesElement(AllWavesElement first, AllWavesElement second)
        {
            Value = AllWavesElement.PackSettings(first, second);
        }

        [field: SerializeField] public float4x4 Value { get; set; }
        public float3 StartFreqPhaseAmp => Value.c0.xzy;
        public AnimationCurve AnimationCurve => (AnimationCurve)(int)Value.c0.w;
        public float4 StartWaveTypeRatio => Value.c1;
        public float3 EndFreqPhaseAmp => Value.c2.xzy;
        public float4 EndWaveTypeRatio => Value.c3;
        public float2 Frequency => new(Value.c0.x, Value.c2.x);
        public float2 Amplitude => new(Value.c0.y, Value.c2.y);
        public float2 Phase => new(Value.c0.z, Value.c2.z);

        private float GetAnimationTime(float lerpTime) => AnimationCurve.GetLerpParameter(lerpTime);
        public float LerpFrequency(float lerpTime) => math.lerp(Value.c0.x, Value.c2.x, GetAnimationTime(lerpTime));
        public float LerpAmplitude(float lerpTime) => math.lerp(Value.c0.y, Value.c2.y, GetAnimationTime(lerpTime));
        public float LerpPhase(float lerpTime) => math.lerp(Value.c0.z, Value.c2.z, GetAnimationTime(lerpTime));

        public float PhaseDelta(float lerpTime, float deltaTime) =>
            (LerpPhase(lerpTime - deltaTime) - LerpPhase(lerpTime + deltaTime)) / (2 * deltaTime);

        public float ValueAtPhase(float phase, float lerpTime)
        {
            float sineValue = math.sin(phase);

            float2 amplitudes = Amplitude;
            float amplitude = math.lerp(amplitudes.x, amplitudes.y, lerpTime);
            float4 waveTypeRatio = math.lerp(StartWaveTypeRatio, EndWaveTypeRatio, lerpTime);

            float squareFactor = math.sign(sineValue);
            float triangleFactor = TwoOverPI * math.asin(sineValue);
            float sawToothFactor = -TwoOverPI * math.atan(1.0f / math.tan(0.5f * (phase - PIOverTwo) - math.PI / 4));

            return math.dot(waveTypeRatio,
                            amplitude * new float4(sineValue, squareFactor, triangleFactor, sawToothFactor));
        }

        // x(-1) - (x+1) / 2

        // col 0: freq, amplitude, phase, animation
        // col 1: (sine amount, square amount, triangle amount, sawtooth amount)
        // col 2: freq, amplitude, phase, <empty>
        // col 3: (sine amount, square amount, triangle amount, sawtooth amount)

        public AllWavesElement CaptureToAllWavesElement(float lerpTime, CurrentWaveAxes axis)
        {
            float time = GetAnimationTime(lerpTime);
            return new()
            {
                Frequency = Frequency.LerpBetween(time),
                Amplitude = Amplitude.LerpBetween(time),
                WaveTypeRatio = math.lerp(StartWaveTypeRatio, EndWaveTypeRatio, time),
                Phase = Phase.LerpBetween(time),
                DisplacementAxis = math.lerp(axis.Start, axis.End, time),
                AnimationCurve = AnimationCurve,
            };
        }
    }
}