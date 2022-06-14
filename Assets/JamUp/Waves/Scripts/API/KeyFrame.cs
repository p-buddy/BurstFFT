namespace JamUp.Waves.Scripts.API
{
    public readonly struct KeyFrame
    {
        public int SampleRate { get; }
        public float Time { get; }
        public float Thickness { get; }
        public float Duration { get; }
        public WaveState[] Waves { get; }
    }
}