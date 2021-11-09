using JamUp.Math;
using Unity.Mathematics;

namespace JamUp.FFT
{
    public readonly struct FFTSize
    {
        public int Width { get; }
        public int Log { get; }
        public FFTSize(int width)
        {
            Width = width;
            Log = (int)math.log2(Width);
        }
    }
}