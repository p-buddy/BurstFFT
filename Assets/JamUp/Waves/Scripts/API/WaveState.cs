using Unity.Mathematics;

namespace JamUp.Waves.Scripts.API
{
    public struct WaveState
    {
        public float Frequency { get; set; }
        public float Amplitude { get; set; }
        public WaveType WaveType { get; set; }
        public float PhaseDegrees { get; set; }
        public SimpleFloat3 DisplacementAxis { get; set; }

        public static implicit operator Wave(WaveState state) =>
            new Wave(state.WaveType, state.Frequency, math.radians(state.PhaseDegrees), state.Amplitude);

    }
}