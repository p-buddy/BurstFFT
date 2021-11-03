using Unity.Collections;
using Unity.Mathematics;

namespace FFT
{
    internal interface IFirstPassJob<T> where T : struct
    {
        NativeArray<T> Input { set; }
        NativeArray<int2> Permutations { set; }
        NativeArray<float4> X { set; }
    }
}