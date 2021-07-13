using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace FFT
{
    [BurstCompile]
    public struct ConjugateAndScaleJob : IJobParallelFor
    {
        public NativeArray<ComplexBin> Bins;
    
        [ReadOnly]
        public float Scale;
        
        public void Execute(int index)
        {
            Bins[index] = Bins[index].Conjugate * Scale;
        }
    }
}