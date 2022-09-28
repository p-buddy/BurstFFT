using Unity.Mathematics;

namespace JamUp.Waves.Scripts.API
{
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
        
        public static implicit operator Wave(WaveState state) =>
            new (state.WaveType, state.Frequency, math.radians(state.PhaseDegrees), state.Amplitude);
    }
}