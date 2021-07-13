using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace FFT
{
    [BurstCompile]
    public struct BinsToUnsignedSamplesJob : IJobParallelFor
    {
        [ReadOnly] 
        public NativeArray<ComplexBin> Bins;
        
        [WriteOnly]
        public NativeArray<float> Samples;
        
        public void Execute(int index)
        {
            Samples[index] = Bins[index].Magnitude;
        }
    }
}