using System.Linq;
using Unity.Collections;

namespace JamUp.Waves.RuntimeScripts.API
{
    public readonly struct KeyFrame
    {
        public float Duration { get; }
        public Animatable<ProjectionType> ProjectionType { get; }
        public Animatable<int> SampleRate { get; }
        public Animatable<float> SignalLength { get; }
        public Animatable<float> Thickness { get; }
        public Animatable<WaveState>[] Waves { get; }

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
            ProjectionType = new (projectionType);
            SignalLength = new(signalLength);
            Thickness = new(thickness);
            Duration = duration;
            Waves = waves.Select(wave => new Animatable<WaveState>(wave)).ToArray();
        }

        internal Animatable<float> ProjectionFloat => new ((int)ProjectionType.Value, ProjectionType.AnimationCurve);
        
        public KeyFrame DefaultAnimations(float duration) => new(duration,
                                                                 SampleRate.Value,
                                                                 ProjectionType.Value,
                                                                 Thickness.Value,
                                                                 Waves.Select(wave => wave.Value).ToArray(),
                                                                 SignalLength.Value);
        
        public KeyFrame ZeroAmplitudes() => new(Duration,
                                                SampleRate.Value,
                                                ProjectionType.Value,
                                                Thickness.Value,
                                                Waves.Select(wave => wave.Value.ZeroedAmplitude).ToArray(),
                                                SignalLength.Value);
    }
}