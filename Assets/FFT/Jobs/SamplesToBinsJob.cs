using Unity.Burst;
using Unity.Jobs;

namespace FFT
{
    [BurstCompile]
    public struct SamplesToBinsJob : IJobParallelFor
    {
        public void Execute(int index)
        {
            throw new System.NotImplementedException();
        }
    }
}