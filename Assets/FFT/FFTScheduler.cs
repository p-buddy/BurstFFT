using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace FFT
{
    public static class FFTScheduler
    {
        private static readonly Dictionary<int, NativeArray<int2>> PermutationTablesByLength = new Dictionary<int, NativeArray<int2>>();
        private static readonly Dictionary<int, NativeArray<TwiddleFactor>> TwiddleFactorTablesByLength = new Dictionary<int, NativeArray<TwiddleFactor>>();
        
        public static void Transform(NativeArray<float> samples)
        {
            
        }

        public static void InverseTransform(NativeArray<ComplexBin> frequencyBins)
        {
            
        }
    }
}