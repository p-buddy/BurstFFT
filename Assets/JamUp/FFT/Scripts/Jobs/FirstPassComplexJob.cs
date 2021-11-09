using JamUp.Math;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace JamUp.FFT
{
    [BurstCompile]
    internal struct FirstPassComplexJob : IJobParallelFor, IFirstPassJob<Complex>
    {
        public NativeArray<Complex> Input
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
        private NativeArray<Complex> input;

        [ReadOnly] 
        private NativeArray<int2> permutations;
        
        [WriteOnly]
        private NativeArray<float4> x;

        public void Execute(int i)
        {
            Complex combined = input[permutations[i].x] + input[permutations[i].y];
            Complex difference = input[permutations[i].x] - input[permutations[i].y];

            x[i] = math.float4(combined.Vector, difference.Vector);
        }
    }
}