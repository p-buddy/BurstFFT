using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace FFT
{
    [BurstCompile]
    internal struct IterativePassJob : IJobParallelFor
    {
        [ReadOnly] 
        internal NativeSlice<TwiddleFactor> TwiddleFactors;
        
        [NativeDisableParallelForRestriction] 
        internal NativeArray<float4> X;

        private static float4 Mulc(float4 a, float4 b) => a.xxzz * b.xyzw + math.float4(-1, 1, -1, 1) * a.yyww * b.yxwz;

        public void Execute(int i)
        {
            var t = TwiddleFactors[i];
            var e = X[t.I1];
            var o = Mulc(t.W4, X[t.I2]);
            X[t.I1] = e + o;
            X[t.I2] = e - o;
        }
    }
}