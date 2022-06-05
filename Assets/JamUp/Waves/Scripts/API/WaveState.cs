using JamUp.Waves;
using Unity.Mathematics;

namespace JamUp.Waves
{
    public struct WaveState
    {
        public float Frequency { get; set; }
        public float Amplitude { get; set; }
        public WaveType WaveType { get; set; }
        public float PhaseDegrees { get; set; }
        public SimpleFloat3 DisplacementAxis { get; set; }
    }
}