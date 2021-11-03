using Unity.Mathematics;

namespace FFT
{
    internal readonly struct TwiddleFactor
    {
        private int2 I { get; }
        private float2 W { get; }

        public int I1 => I.x;
        public int I2 => I.y;

        public float4 W4 => math.float4(W.x, math.sqrt(1 - W.x * W.x), W.y, math.sqrt(1 - W.y * W.y));

        public TwiddleFactor(int2 i, float2 w)
        {
            I = i;
            W = w;
        }
        
    }
}