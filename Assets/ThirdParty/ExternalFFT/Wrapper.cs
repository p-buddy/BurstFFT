namespace JamUp.ThirdParty.ExternalFFT
{
    public static class Wrapper
    {
        public static float[] TimeToFrequency(float[] samplesIn)
        {
            Complex[] complexSamplesIn = FFT.Float2Complex(samplesIn);
            Complex[] complexSamplesOut = FFT.CalculateFFT(complexSamplesIn, false);
            return FFT.Complex2Float(complexSamplesOut, false);
        }
    }
}