using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace FFT
{
    [BurstCompile]
    internal struct ComplexToUnsignedSamplesJob : IJobParallelFor
    {
        [ReadOnly] 
        internal NativeArray<Complex> Bins;
        
        [WriteOnly]
        internal NativeArray<float> Samples;
        
        public void Execute(int index)
        {
            Samples[index] = Bins[index].Magnitude;
        }
    }
}