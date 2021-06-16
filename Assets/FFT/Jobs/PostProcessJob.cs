using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace FFT
{
    struct PostprocessJob : IJobParallelFor
    {
        [ReadOnly] 
        public NativeArray<float4> X;
        
        [WriteOnly] 
        public NativeArray<ComplexBin> O;
        
        [ReadOnly]
        public float Scale;

        public void Execute(int i)
        {
            float4 x = X[i];
            int outputIndex = i * 2;
            O[outputIndex] = new ComplexBin(x.xy * Scale);
            O[outputIndex + 1] = new ComplexBin(x.zw * Scale);
        }
    }
}