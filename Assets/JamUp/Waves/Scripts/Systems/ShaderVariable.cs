namespace JamUp.Waves.Scripts
{
    public enum ShaderVariable
    {
        WaveCount,
        StartTime,
        EndTime,
        Projection,
        Thickness,
        SampleRate,
        SignalLength,
    }

    public static class ShaderVariableHelper
    {
        public static string Name(this ShaderVariable v)
        {
            return "";
        }
    }
}