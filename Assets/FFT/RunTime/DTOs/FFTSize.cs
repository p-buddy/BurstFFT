using Unity.Mathematics;

namespace FFT
{
    public readonly struct FFTSize
    {
        public int Width { get; }
        public int Log { get; }

        public FFTSize(int width)
        {
            Width = width;
            Log = (int)math.log2(width);
        }
    }
}