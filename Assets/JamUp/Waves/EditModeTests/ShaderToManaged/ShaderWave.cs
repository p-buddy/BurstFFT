using System.Runtime.InteropServices;
using JamUp.StringUtility;

namespace JamUp.Waves
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ShaderWave
    {
        public float Frequency;
        public float Amplitude;
        public float PhaseRadians;
        public int WaveType;

        public override string ToString()
        {
            return this.NameAndPublicData(true);
        }
        
        #region Shader Debugging / Testing
        public Wave ToWaveManagedWave() => new Wave((WaveType)WaveType, Frequency, PhaseRadians, Amplitude);

        public static ShaderWave FromManagedWave(in Wave wave) => new ShaderWave
        {
            Frequency = wave.Frequency,
            Amplitude = wave.Amplitude,
            PhaseRadians = wave.PhaseOffset,
            WaveType = (int)wave.WaveType,
        };
        #endregion Shader Debugging / Testing
    }
}