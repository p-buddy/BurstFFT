using Unity.Mathematics;

namespace JamUp.Waves
{
    public readonly struct Wave
    {
        public WaveType WaveType { get; }
        public float Frequency { get; }
        public float PhaseOffset { get; }
        public float Amplitude { get; }

        public Wave(WaveType waveType, float frequency, float phaseOffset = 0f, float amplitude = 1f)
        {
            WaveType = waveType;
            Frequency = frequency;
            PhaseOffset = phaseOffset;
            Amplitude = amplitude;
        }

        public static Wave Lerp(in Wave start, in Wave end, float s)
        {
            return new Wave(start.WaveType,
                            math.lerp(start.Frequency, end.Frequency, s),
                            math.lerp(start.PhaseOffset, end.PhaseOffset, s));
        }

        #if UNITY_EDITOR
        public override string ToString()
        {
            return JamUp.StringUtility.ToStringHelper.NameAndPublicData(this, true);
        }
        #endif 
    }
}