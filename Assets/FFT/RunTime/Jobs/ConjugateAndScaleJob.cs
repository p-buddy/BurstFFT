using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace FFT
{
    [BurstCompile]
    internal struct ConjugateAndScaleJob : IJobParallelFor
    {
        internal NativeArray<Complex> Bins;
    
        [ReadOnly]
        internal float Scale;
        
        public void Execute(int index)
        {
            Bins[index] = Bins[index].Conjugate * Scale;
        }
    }
}