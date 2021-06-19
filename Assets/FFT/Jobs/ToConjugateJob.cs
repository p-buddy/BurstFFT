using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace FFT
{
    [BurstCompile]
    public struct ToConjugateJob : IJobParallelFor
    {
        public NativeArray<ComplexBin> Bins;
        
        public void Execute(int index)
        {
            Bins[index] = Bins[index].Conjugate;
        }
    }
}