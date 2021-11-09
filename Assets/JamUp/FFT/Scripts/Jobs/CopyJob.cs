using Unity.Collections;
using Unity.Jobs;

namespace JamUp.FFT
{
    public struct CopyJob<T> : IJobParallelFor where T : struct
    {
        [ReadOnly]
        public NativeArray<T> From;
        
        [WriteOnly]
        public NativeArray<T> To;
        
        public void Execute(int index)
        {
            To[index] = From[index];
        }
    }
}