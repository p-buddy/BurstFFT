using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor.UIElements;
using UnityEngine;

namespace FFT
{
    public static class FFTScheduler
    {
        const int InnerLoopBatchCount = 32;

        #region MyRegion
        private static readonly Dictionary<int, NativeArray<int2>> PermutationTablesByLength = new Dictionary<int, NativeArray<int2>>();
        private static readonly Dictionary<int, NativeArray<TwiddleFactor>> TwiddleFactorTablesByLength = new Dictionary<int, NativeArray<TwiddleFactor>>();

        private static ConcurrentQueue<NativeArray<ComplexBin>> ComplexBinsPool = new ConcurrentQueue<NativeArray<ComplexBin>>();
        private static ConcurrentQueue<NativeArray<float>> SamplesPool = new ConcurrentQueue<NativeArray<float>>();
        #endregion

        #region MyRegion
        private static NativeArray<int2> GetPermutations(in FFTSize size, out JobHandle handle)
        {
            if (!PermutationTablesByLength.TryGetValue(size.Width, out NativeArray<int2> permutations))
            {
                var table = new NativeArray<int2>(size.Width / 2, Allocator.Persistent);
                handle = new BuildPermutationsTableJob
                {
                    PermutationsTable = table,
                    Size = size
                }.Schedule();

                PermutationTablesByLength[size.Width] = table;
                return table;
            }

            handle = default;
            return permutations;
        }
        
        private static NativeArray<TwiddleFactor> GetTwiddleFactors(in FFTSize size, out JobHandle handle)
        {
            if (!TwiddleFactorTablesByLength.TryGetValue(size.Width, out NativeArray<TwiddleFactor> twiddleFactors))
            {
                var table = new NativeArray<TwiddleFactor>((size.Log - 1) * (size.Width / 4), Allocator.Persistent);
                handle = new BuildTwiddleFactorsJob
                {
                    TwiddleFactors = table,
                    fftLength = size.Width
                }.Schedule();

                TwiddleFactorTablesByLength[size.Width] = table;
                return table;
            }

            handle = default;
            return twiddleFactors;
        }

        private static NativeArray<float4> GetFFTInputArray(in FFTSize size, Allocator allocator)
        {
            return new NativeArray<float4>(size.Width / 2, allocator);
        }
        #endregion

        private static FFTOutput<ComplexBin> Phase2(in FFTSize size, JobHandle handle, NativeArray<float4> X)
        {
            NativeArray<TwiddleFactor> twiddleFactors = GetTwiddleFactors(in size, out JobHandle twiddleFactorHandle);
            JobHandle combinedHandle = JobHandle.CombineDependencies(twiddleFactorHandle, handle);
            for (var i = 0; i < size.Log - 1; i++)
            {
                var twiddleFactorsSubSection = new NativeSlice<TwiddleFactor>(twiddleFactors, size.Width / 4 * i);
                combinedHandle = new IterativePassJob
                {
                    TwiddleFactors = twiddleFactorsSubSection, 
                    X = X
                }.Schedule(size.Width / 4, InnerLoopBatchCount, combinedHandle);
            }
            
            var complexOutput = X.Reinterpret<ComplexBin>(sizeof(float) * 4);
            JobHandle finalPassHandle = new PostprocessJob
            {
                Output = complexOutput,
                Scale = 2.0f / size.Width
            }.Schedule(size.Width, InnerLoopBatchCount, combinedHandle);
            
            return new FFTOutput<ComplexBin>
            {
                Data = complexOutput,
                Handle = finalPassHandle
            };
        }
        
        public static FFTOutput<ComplexBin> TransformToBins(FFTInput<float> input, bool internalCall = false)
        {
            FFTSize size = input.Size;
            NativeArray<int2> permutations = GetPermutations(in size, out JobHandle permutationsHandle);
            
            NativeArray<float4> x = GetFFTInputArray(in size, Allocator.Persistent);
            JobHandle firstPassHandle = new FirstPassFloatJob
            {
                Input = input.Data, 
                Permutations = permutations, 
                X = x
            }.Schedule(size.Width / 2, InnerLoopBatchCount, permutationsHandle);

            FFTOutput<ComplexBin> output = Phase2(in size, firstPassHandle, x);
            
            if (!internalCall)
            {
                x.Dispose(output.Handle);
            }

            return new FFTOutput<ComplexBin>
            {
                Data = output.Data,
                Handle = output.Handle
            };
        }

        public static FFTOutput<float> TransformToSamples(FFTInput<float> input)
        {
            FFTSize size = input.Size;
            FFTOutput<ComplexBin> bins = TransformToBins(input, true);
            NativeArray<float> samples = new NativeArray<float>(input.Size.Width, Allocator.Persistent);
            
            JobHandle handle = new BinsToSamplesJob()
            {
                Samples = samples,
                Bins = bins.Data
            }.Schedule(size.Width, InnerLoopBatchCount, bins.Handle);

            bins.Data.Dispose(handle);
            
            var z = new FFTOutput<float>
            {
                Data = samples,
                Handle = handle
            };
            return z;
        }
        
        /*
        public static FFTOutput<float> InverseTransformToSamples(NativeArray<ComplexBin> frequencyBins)
        {
        }
        */
    }
}