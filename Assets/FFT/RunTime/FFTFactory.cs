using System;
using System.Collections.Generic;

using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace FFT
{
    /// <summary>
    /// 
    /// </summary>
    public static class FFTFactory
    {
        const int InnerLoopBatchCount = 32;

        #region Fields
        private static readonly Dictionary<int, NativeArray<int2>> PermutationTablesByLength;
        private static readonly Dictionary<int, NativeArray<TwiddleFactor>> TwiddleFactorTablesByLength;
        #endregion Fields

        static FFTFactory()
        {
            PermutationTablesByLength = new Dictionary<int, NativeArray<int2>>();
            TwiddleFactorTablesByLength = new Dictionary<int, NativeArray<TwiddleFactor>>();
        }

        public static void Dispose(JobHandle dependency = default)
        {
            foreach (NativeArray<int2> nativeArray in PermutationTablesByLength.Values)
            {
                nativeArray.Dispose(dependency);
            }
            foreach (NativeArray<TwiddleFactor> nativeArray in TwiddleFactorTablesByLength.Values)
            {
                nativeArray.Dispose(dependency);
            }
        }
        
        private static void Destruct() 
        {
            Dispose();
        }

        #region Internal Helper Functions
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
                    FFTLength = size.Width
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

        private static Samples<Complex> Phase2(in FFTSize size, JobHandle dependencies, NativeArray<float4> complexPairs)
        {
            NativeArray<TwiddleFactor> twiddleFactors = GetTwiddleFactors(in size, out JobHandle twiddleFactorHandle);
            JobHandle handle = JobHandle.CombineDependencies(twiddleFactorHandle, dependencies);
            for (var i = 0; i < size.Log - 1; i++)
            {
                var twiddleFactorsSubSection = new NativeSlice<TwiddleFactor>(twiddleFactors, size.Width / 4 * i);
                handle = new IterativePassJob
                {
                    TwiddleFactors = twiddleFactorsSubSection,
                    X = complexPairs
                }.Schedule(size.Width / 4, InnerLoopBatchCount, handle);
            }

            NativeArray<Complex> complexOutput = complexPairs.Reinterpret<Complex>(sizeof(float) * 4);
            handle = new PostprocessJob
            {
                Output = complexOutput,
                Scale = 2.0f / size.Width
            }.Schedule(size.Width, InnerLoopBatchCount, handle);

            return new Samples<Complex>(complexOutput, handle);
        }

        private static Samples<Complex> TransformToComplexFrom<TData, TJob>(in Samples<TData> input,
                                                                            Allocator allocator,
                                                                            JobHandle dependency)
            where TData : struct
            where TJob : struct, IFirstPassJob<TData>, IJobParallelFor
        {
            FFTSize size = new FFTSize(input.Length);
            NativeArray<int2> permutations = GetPermutations(in size, out JobHandle handle);
            
            NativeArray<float4> complexPairs = GetFFTInputArray(in size, allocator);
            handle = new TJob
            {
                Input = input.DeferredData, 
                Permutations = permutations, 
                X = complexPairs
            }.Schedule(size.Width / 2, InnerLoopBatchCount, JobHandle.CombineDependencies(handle, input.Handle, dependency));
            
            return Phase2(in size, handle, complexPairs);
        }

        private static Samples<float> ToTimeSamples(this in Samples<Complex> complexSampleOutput,
                                                    bool disposeBins,
                                                    Allocator allocator,
                                                    JobHandle dependency)
        {
            NativeArray<float> samples = new NativeArray<float>(complexSampleOutput.Length, allocator);
            NativeArray<Complex> binData = complexSampleOutput.DeferredData;
            JobHandle handle = new ComplexToSignedSamplesJob()
            {
                Samples = samples,
                Bins = binData
            }.Schedule(samples.Length, InnerLoopBatchCount, JobHandle.CombineDependencies(complexSampleOutput.Handle, dependency));

            if (disposeBins)
            {
                binData.Dispose(handle);
            }

            return new Samples<float>(samples, handle);
        }
        #endregion Internal Helper Functions

        #region Public Functions
        /// <summary>
        /// 
        /// </summary>
        /// <param name="samplesIn"></param>
        /// <param name="samplesOut"></param>
        /// <param name="allocator"></param>
        /// <param name="dependency"></param>
        public static void TransformFromTimeToFrequency(this in Samples<float> samplesIn,
                                                        out Samples<Complex> samplesOut,
                                                        Allocator allocator,
                                                        JobHandle dependency = default)
        {
            samplesOut = TransformToComplexFrom<float, FirstPassFloatJob>(in samplesIn, allocator, dependency);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="samplesIn"></param>
        /// <param name="samplesOut"></param>
        /// <param name="allocator"></param>
        /// <param name="dependency"></param>
        public static void TransformFromTimeToFrequency(this in Samples<float> samplesIn,
                                                        out Samples<float> samplesOut,
                                                        Allocator allocator,
                                                        JobHandle dependency = default)
        {
            TransformFromTimeToFrequency(samplesIn, out Samples<Complex> complexBins, allocator, dependency);
            CollapseToFrequencyBins(complexBins, out samplesOut, true, allocator, complexBins.Handle);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="complexBins"></param>
        /// <param name="frequencyBins"></param>
        /// <param name="disposeBins"></param>
        /// <param name="allocator"></param>
        /// <param name="dependency"></param>
        public static void CollapseToFrequencyBins(this in Samples<Complex> complexBins,
                                                   out Samples<float> frequencyBins,
                                                   bool disposeBins,
                                                   Allocator allocator,
                                                   JobHandle dependency = default)
        {
            NativeArray<float> binSamples = new NativeArray<float>(complexBins.Length, allocator);
            JobHandle handle = new ComplexToUnsignedSamplesJob()
            {
                Samples = binSamples,
                Bins = complexBins.DeferredData
            }.Schedule(binSamples.Length, InnerLoopBatchCount, JobHandle.CombineDependencies(complexBins.Handle, dependency));

            if (disposeBins)
            {
                complexBins.Dispose(handle);
            }

            frequencyBins = new Samples<float>(binSamples, handle);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="samplesIn"></param>
        /// <param name="samplesOut"></param>
        /// <param name="allocator"></param>
        /// <param name="dependency"></param>
        public static void InverseTransformFromFrequencyToTime(this in Samples<Complex> samplesIn,
                                                               out Samples<Complex> samplesOut,
                                                               Allocator allocator,
                                                               JobHandle dependency = default)
        {
            int length = samplesIn.Length;
            JobHandle handle = new ConjugateJob
            {
                Bins = samplesIn.DeferredData
            }.Schedule(length, InnerLoopBatchCount, JobHandle.CombineDependencies(samplesIn.Handle, dependency));

            Samples<Complex> complexDataIn = new Samples<Complex>(samplesIn.DeferredData, handle);
            Samples<Complex> complexDataOut = TransformToComplexFrom<Complex, FirstPassComplexJob>(in complexDataIn, allocator, handle);
            
            JobHandle conjugateAndScaleHandle = new ConjugateAndScaleJob
            {
                Bins = complexDataOut.DeferredData,
                Scale = length / 4.0f // This 4 is a magic number -- must figure out later!
            }.Schedule(length, InnerLoopBatchCount, complexDataOut.Handle);

            samplesOut = new Samples<Complex>(complexDataOut.DeferredData, conjugateAndScaleHandle);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="samplesIn"></param>
        /// <param name="samplesOut"></param>
        /// <param name="allocator"></param>
        /// <param name="dependency"></param>
        public static void InverseTransformFromFrequencyToTime(this in Samples<Complex> samplesIn,
                                                               out Samples<float> samplesOut,
                                                               Allocator allocator,
                                                               JobHandle dependency = default)
        {
            InverseTransformFromFrequencyToTime(in samplesIn, out Samples<Complex> complexSamples, allocator, dependency);
            samplesOut = ToTimeSamples(complexSamples, true, allocator, complexSamples.Handle);
        }
        #endregion Public Functions
    }
}