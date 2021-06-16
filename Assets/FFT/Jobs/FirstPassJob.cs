using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace FFT
{
    [BurstCompile]
    public struct FirstPassJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<ComplexBin> Input;
        [ReadOnly] 
        public NativeArray<int2> Permutations;
        [WriteOnly] 
        public NativeArray<float4> X;

        public void Execute(int i)
        {
            ComplexBin combined = Input[Permutations[i].x] + Input[Permutations[i].y];
            ComplexBin difference = Input[Permutations[i].x] - Input[Permutations[i].y];

            X[i] = math.float4(combined.Vector, difference.Vector);
        }
    }
    
    
}