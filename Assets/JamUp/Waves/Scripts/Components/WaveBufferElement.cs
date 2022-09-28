using JamUp.Waves.Scripts.API;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace JamUp.Waves.Scripts
{
    public readonly struct WaveBufferElement: IBufferElementData, IAnimatable
    {
        public WaveType WaveType { get; }
        public float Frequency { get; }
        public float PhaseOffset { get; }
        public float Amplitude { get; }
        public float3 DisplacementAxis { get;}
        public AnimationCurve Animation { get; }

        public WaveBufferElement(in AnimatableProperty<WaveState> animatedWave)
        {
            WaveState wave = animatedWave.Value;
            WaveType = wave.WaveType;
            Frequency = wave.Frequency;
            PhaseOffset = math.radians(wave.PhaseDegrees);
            Amplitude = wave.Amplitude;
            DisplacementAxis = wave.DisplacementAxis;
            Animation = animatedWave.Animation;
        }

        public void SetWaveData(NativeArray<float4> waves, NativeArray<float3> axes, int index)
        {
            waves[index] = new float4(Frequency, Amplitude, PhaseOffset, (float)WaveType);
            axes[index] = DisplacementAxis;
        }
    }
}