using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace FFT
{
    [BurstCompile]
    public struct FirstPassComplexJob : IJobParallelFor, IFirstPassJob<ComplexBin>
    {
        public NativeArray<ComplexBin> Input
        {
            set => input = value;
        }

        public NativeArray<int2> Permutations
        {
            set => permutations = value;
        }

        public NativeArray<float4> X
        {
            set => x = value;
        }

        [ReadOnly] 
        private NativeArray<ComplexBin> input;

        [ReadOnly] 
        private NativeArray<int2> permutations;
        
        [WriteOnly]
        private NativeArray<float4> x;

        public void Execute(int i)
        {
            ComplexBin combined = input[permutations[i].x] + input[permutations[i].y];
            ComplexBin difference = input[permutations[i].x] - input[permutations[i].y];

            x[i] = math.float4(combined.Vector, difference.Vector);
        }
    }
}