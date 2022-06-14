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
        public static implicit operator Wave(WaveState state) =>
            new Wave(state.WaveType, state.Frequency, math.radians(state.PhaseDegrees), state.Amplitude);
    }
}