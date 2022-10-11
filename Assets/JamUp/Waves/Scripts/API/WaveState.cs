using Unity.Burst;
using Unity.Mathematics;

namespace JamUp.Waves.Scripts.API
{
    [BurstCompile]
    public readonly struct WaveState
    {
        public float Frequency { get; }
        public float Amplitude { get; }
        public WaveType WaveType { get; }
        public float PhaseDegrees { get; }
        public SimpleFloat3 DisplacementAxis { get; }

        public WaveState(float frequency,
                         float amplitude,
                         float phaseDegrees,
                         WaveType waveType,
                         SimpleFloat3 displacementAxis)
        {
            Frequency = frequency;
            Amplitude = amplitude;
            PhaseDegrees = phaseDegrees;
            WaveType = waveType;
            DisplacementAxis = displacementAxis;
        }

        public WaveState ZeroedAmplitude => new(Frequency, 0f, PhaseDegrees, WaveType, DisplacementAxis);

        public float4 PackSettings => new (Frequency, Amplitude, math.radians(PhaseDegrees), (float)WaveType);
        
        public static implicit operator Wave(WaveState state) =>
            new (state.WaveType, state.Frequency, math.radians(state.PhaseDegrees), state.Amplitude);

        public static float4x4 Pack(in WaveState first, in WaveState second, float m31 = default, float m33 = default) =>
            math.transpose(new float4x4(first.PackSettings,
                                        new float4(first.DisplacementAxis, m31),
                                        second.PackSettings,
                                        new float4(second.DisplacementAxis, m33)));

    }
}