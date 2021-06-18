using Unity.Collections;
using Unity.Jobs;

namespace FFT
{
    public struct FFTOutput<T> where T : struct
    {
        public NativeArray<T> Data;
        public JobHandle Handle;
    }
}