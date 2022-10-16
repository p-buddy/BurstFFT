using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Burst;
using Unity.Mathematics;

namespace JamUp.Waves.RuntimeScripts.API
{
    [BurstCompile]
    public readonly struct WaveState
    {
        public float Frequency { get; }
        public float Amplitude { get; }
        public WaveType WaveType { get; }
        public float PhaseDegrees { get; }
        public Vector DisplacementAxis { get; }

        public WaveState(float frequency,
                         float amplitude,
                         float phaseDegrees,
                         WaveType waveType,
                         Vector displacementAxis)
        {
            Frequency = frequency;
            Amplitude = amplitude;
            PhaseDegrees = phaseDegrees;
            WaveType = waveType;
            DisplacementAxis = displacementAxis;
        }

        [HideFromAPI]
        public WaveState ZeroedAmplitude => new(Frequency, 0f, PhaseDegrees, WaveType, DisplacementAxis);
        
        public static implicit operator Wave(WaveState state) =>
            new (state.WaveType, state.Frequency, math.radians(state.PhaseDegrees), state.Amplitude);

        public AllWavesElement AsWaveElement(AnimationCurve curve = AnimationCurve.Linear) => new()
        {
            Frequency = Frequency,
            Amplitude = Amplitude,
            Phase = math.radians(PhaseDegrees),
            AnimationCurve = curve,
            DisplacementAxis = DisplacementAxis,
            WaveTypeRatio = new float4(Sine, Square, Triangle, Sawtooth),
        };
        
        public AllWavesElement AsWaveElement(float4 waveTypeRatio, AnimationCurve curve = AnimationCurve.Linear) => new()
        {
            Frequency = Frequency,
            Amplitude = Amplitude,
            Phase = math.radians(PhaseDegrees),
            AnimationCurve = curve,
            DisplacementAxis = DisplacementAxis,
            WaveTypeRatio = waveTypeRatio,
        };

        private int Sine => (int)(WaveType.Sine & WaveType);
        private int Square => (int)(WaveType.Square & WaveType) >> 1;
        private int Triangle => (int)(WaveType.Triangle & WaveType) >> 2;
        private int Sawtooth => (int)(WaveType.Sawtooth & WaveType) >> 3;
    }
}