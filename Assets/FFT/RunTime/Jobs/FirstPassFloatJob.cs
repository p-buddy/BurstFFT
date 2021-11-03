using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace FFT
{
    [BurstCompile]
    internal struct FirstPassFloatJob : IJobParallelFor, IFirstPassJob<float>
    {
        public NativeArray<float> Input
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
        private NativeArray<float> input;

        [ReadOnly] 
        private NativeArray<int2> permutations;
        
        [WriteOnly]
        private NativeArray<float4> x;

        public void Execute(int i)
        {
            float combined = input[permutations[i].x] + input[permutations[i].y];
            float difference = input[permutations[i].x] - input[permutations[i].y];

            x[i] = math.float4(combined, 0f, difference, 0f);
        }
    }
}