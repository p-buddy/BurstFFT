using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace FFT
{
    [BurstCompile]
    internal struct PostprocessJob : IJobParallelFor
    {
        internal NativeArray<Complex> Output;
        
        [ReadOnly]
        internal float Scale;

        public void Execute(int i)
        {
            Output[i] = Output[i] * Scale;
        }
    }
}