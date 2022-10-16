using System;
using JamUp.Waves.RuntimeScripts;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.DataVisualization.Waves
{
    [Serializable]
    public struct SerializableWave
    {
        [SerializeField]
        [Range(1, 100f)]
        private float frequency;
        
        [SerializeField]
        [Range(-5f, 5f)]
        private float amplitude;

        [SerializeField] 
        private WaveType waveType;

        [SerializeField] 
        [Range(0f, 360f)]
        private float phaseDegrees;
        
        [SerializeField] 
        private float3 displacementAxis;

        public float3 DisplacementAxis => math.normalize(displacementAxis);
        
        public Wave Wave => new Wave(waveType, frequency, math.radians(phaseDegrees), amplitude);

        public SerializableWave(WaveType waveType, float frequency, float phaseDegrees, float amplitude)
        {
            this.waveType = waveType;
            this.frequency = frequency;
            this.phaseDegrees = phaseDegrees;
            this.amplitude = amplitude;
            displacementAxis = math.up();
        }

        public static implicit operator SerializableWave(Wave wave)
        {
            return new SerializableWave
            {
                frequency = wave.Frequency,
                amplitude = wave.Amplitude,
                waveType = wave.WaveType,
                phaseDegrees = math.degrees(wave.PhaseOffset)
            };
        }
        
        public static bool operator ==(SerializableWave a, SerializableWave b) => a.Wave == b.Wave;
        public static bool operator !=(SerializableWave a, SerializableWave b) => !(a.Wave == b.Wave);
    }
}