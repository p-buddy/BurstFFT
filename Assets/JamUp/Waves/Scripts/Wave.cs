using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using pbuddy.StringUtility.RuntimeScripts;
using Unity.Mathematics;

namespace JamUp.Waves.Scripts
{
    public readonly struct Wave : IEqualityComparer<Wave>
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

        public static Wave SinToCos(in Wave wave)
        {
            return new Wave(wave.WaveType, wave.Frequency, math.PI / 2f, wave.Amplitude);
        }

        public override string ToString()
        {
            return this.NameAndPublicData(true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Wave x, Wave y) => x == y;

        public int GetHashCode(Wave obj)
        {
            unchecked
            {
                var hashCode = (int)obj.WaveType;
                hashCode = (hashCode * 397) ^ obj.Frequency.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.PhaseOffset.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.Amplitude.GetHashCode();
                return hashCode;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Wave x, Wave y) =>
            x.WaveType == y.WaveType &&
            x.Frequency == y.Frequency &&
            x.PhaseOffset == y.PhaseOffset &&
            x.Amplitude == y.Amplitude;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Wave a, Wave b) => !(a == b);
    }
}