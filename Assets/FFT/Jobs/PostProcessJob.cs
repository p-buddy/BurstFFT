using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace FFT
{
    public struct PostprocessJob : IJobParallelFor
    {
        public NativeArray<ComplexBin> Output;
        
        [ReadOnly]
        public float Scale;

        public void Execute(int i)
        {
            Output[i] = Output[i] * Scale;
        }
    }
}