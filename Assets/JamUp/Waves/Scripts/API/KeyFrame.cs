namespace JamUp.Waves.Scripts.API
{
    public readonly struct KeyFrame
    {
        public Projection Projection { get; }
        public int SampleRate { get; }
        public float Time { get; }
        public float Thickness { get; }
        public float Duration { get; }
        public WaveState[] Waves { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="sampleRate"></param>
        /// <param name="projection"></param>
        /// <param name="thickness"></param>
        /// <param name="waves"></param>
        /// <param name="time"></param>
        public KeyFrame(float duration, int sampleRate, Projection projection, float thickness, WaveState[] waves, float time)
        {
            SampleRate = sampleRate;
            Projection = projection;
            Time = time;
            Thickness = thickness;
            Duration = duration;
            Waves = waves;
        }
    }
}