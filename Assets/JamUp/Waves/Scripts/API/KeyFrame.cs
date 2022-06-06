namespace JamUp.Waves.Scripts.API
{
    public struct KeyFrame
    {
        public int SampleRate { get; set; }
        public float Time { get; set; }
        public float Thickness { get; set; }
        public float Duration { get; set; }
        public WaveState[] Waves { get; set; }
    }
}