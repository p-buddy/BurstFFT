using System.Linq;
using Unity.Collections;

namespace JamUp.Waves.Scripts.API
{
    public readonly struct KeyFrame
    {
        public float Duration { get; }
        public AnimatableProperty<float> ProjectionType { get; }
        public AnimatableProperty<float> SampleRate { get; }
        public AnimatableProperty<float> SignalLength { get; }
        public AnimatableProperty<float> Thickness { get; }
        public AnimatableProperty<WaveState>[] Waves { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="duration">
        /// En: Hello
        /// </param>
        /// <param name="sampleRate"></param>
        /// <param name="projectionType"></param>
        /// <param name="thickness"></param>
        /// <param name="waves"></param>
        /// <param name="signalLength"></param>
        public KeyFrame(float duration,
                        int sampleRate,
                        ProjectionType projectionType,
                        float thickness,
                        WaveState[] waves,
                        float signalLength)
        {
            SampleRate = new(sampleRate);
            ProjectionType = new ((int)projectionType);
            SignalLength = new(signalLength);
            Thickness = new(thickness);
            Duration = duration;
            Waves = waves.Select(wave => new AnimatableProperty<WaveState>(wave)).ToArray();
        }
        
        internal KeyFrame(float duration,
                          float sampleRate,
                          float projectionType,
                          float thickness,
                          WaveState[] waves,
                          float signalLength)
        {
            SampleRate = new(sampleRate);
            ProjectionType = new (projectionType);
            SignalLength = new(signalLength);
            Thickness = new(thickness);
            Duration = duration;
            Waves = waves.Select(wave => new AnimatableProperty<WaveState>(wave)).ToArray();
        }

        public struct JobFriendlyRepresentation
        {
            public NativeArray<AnimatableProperty<float>> Projections;
            public NativeArray<AnimatableProperty<float>> SampleRates;
            public NativeArray<AnimatableProperty<float>> SignalLengths;
            public NativeArray<AnimatableProperty<float>> Thicknesses;
            public NativeArray<float> Durations;
            public NativeArray<int> WaveCounts;
        }        
        
        public void CaptureForJob(JobFriendlyRepresentation representation, int index)
        {
            representation.Projections[index] = ProjectionType;
            representation.SampleRates[index] = SampleRate;
            representation.SignalLengths[index] = SignalLength;
            representation.Thicknesses[index] = Thickness;
            representation.Durations[index] = Duration;
            representation.WaveCounts[index] = Waves.Length;
        }

        public void CaptureWaves(NativeArray<AnimatableProperty<WaveState>> waves, int index)
        {
            NativeArray<AnimatableProperty<WaveState>>.Copy(waves, 0, Waves, index, Waves.Length);
        }
    }
}