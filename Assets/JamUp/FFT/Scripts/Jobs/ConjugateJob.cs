using JamUp.Math;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace JamUp.FFT
{
    [BurstCompile]
    internal struct ConjugateJob : IJobParallelFor
    {
        internal NativeArray<Complex> Bins;
        
        public void Execute(int index)
        {
            Bins[index] = Bins[index].Conjugate;
        }
    }
}