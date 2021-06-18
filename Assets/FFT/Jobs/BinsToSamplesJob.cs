using Unity.Collections;
using Unity.Jobs;

namespace FFT
{
    public struct BinsToSamplesJob : IJobParallelFor
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