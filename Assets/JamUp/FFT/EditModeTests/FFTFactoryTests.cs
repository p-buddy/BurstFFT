using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

using Unity.Collections;
using Unity.Jobs;
using Random = Unity.Mathematics.Random;

using JamUp.Math;
using JamUp.Waves;
using JamUp.NativeArrayUtility;
using JamUp.TestUtility;
using Unity.Mathematics;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

using static JamUp.ThirdParty.ExternalFFT.Wrapper;

namespace JamUp.FFT.EditModeTests
{
    public class FFTFactoryTests : TestBase
    {
        private const int SampleRate = 44100;
        private const int SampleLength = 256;
        private const int BatchCount = SampleLength / 8;
        
        private WaveType[] waveTypes = Enum.GetValues(typeof(WaveType)).Cast<WaveType>().ToArray();

        [Test]
        public void OursVsTheirs()
        {
            const int numberOfWaves = 10;
            Random random = new Random(1);
            float[] frequencies = new float[numberOfWaves];
            float[] phases = new float[numberOfWaves];
            float[] amplitudes = new float[numberOfWaves];
            Wave[] waves = new Wave[numberOfWaves];

            for (var index = 0; index < numberOfWaves; index++)
            {
                frequencies[index] = FFTFactory.BinIndexToFrequency(random.NextInt(0, SampleLength), SampleLength, SampleRate);
                phases[index] = 0f;//math.radians(random.NextFloat(0, 360f));
                amplitudes[index] = 1f;//random.NextFloat(0f, 1f);
                waves[index] = new Wave(WaveType.Sine, frequencies[index], phases[index], amplitudes[index]);
            }
            
            // sort test frequencies
            NativeArray<float> sortedFrequencies = new NativeArray<float>(frequencies, Allocator.Temp);
            sortedFrequencies.Sort();
            frequencies = sortedFrequencies.ToArray();
            sortedFrequencies.Dispose();
            
            float[] frequencyBinValues = FFTFactory.GetFrequencyBinValues(SampleLength, SampleRate);

            (float below, float above) GetClosestBinFrequencies(float frequency)
            {
                for (var index = 0; index < frequencyBinValues.Length; index++)
                {
                    if (frequencyBinValues[index] >= frequency)
                    {
                        int previousIndex = index > 0 ? index - 1 : 0;
                        return (frequencyBinValues[previousIndex], frequencyBinValues[index]);
                    }
                }

                return (-1f, -1f);
            }
            
            float GetClosestTestFrequency(float binFrequency)
            {
                for (var index = 0; index < frequencies.Length; index++)
                {
                    if (frequencies[index] >= binFrequency)
                    {
                        float distFromLast = math.distance(frequencies[index - 1], binFrequency);
                        float distToThis =  math.distance(frequencies[index], binFrequency);
                        return distFromLast < distToThis ? frequencies[index - 1] : frequencies[index];
                    }
                }

                return frequencies.Last();
            }

            Debug.Log($"Frequencies: [{String.Join(", ", frequencies)}]");
            Debug.Log($"Closest Bin Frequencies: [{String.Join(", ", frequencies.ToList().Select(GetClosestBinFrequencies))}]");
            
            NativeArray<float> data = WaveDataFactory.GetRealValueArray(waves,
                                                                    SampleLength,
                                                                    SampleRate,
                                                                    Allocator.TempJob,
                                                                    out JobHandle jobHandle);
            
            Samples<float> ourOutput = GetFrequencyResponse(data, jobHandle);
            NativeArray<SortedWithIndex<float>> ourSortedIndices = SortUtility.GetSortedIndices(ourOutput.DeferredData,
                                                                                                  Allocator.TempJob,
                                                                                                  out JobHandle ourSortHandle,
                                                                                                  ourOutput.Handle);
            
            jobHandle.Complete();
            float[] theirOutput = TimeToFrequency(data.ToArray());
            data.Dispose(ourOutput.Handle);
            NativeArray<SortedWithIndex<float>> theirSortedIndices = SortUtility.GetSortedIndices(theirOutput,
                                                                                                  Allocator.TempJob,
                                                                                                  out JobHandle theirSortHandle);
            JobHandle.CombineDependencies(ourSortHandle, theirSortHandle).Complete();
            
            Assert.AreEqual(ourSortedIndices.Length, theirSortedIndices.Length);

            for (int i = 0; i < numberOfWaves * 2; i++)
            {
                float ours = FFTFactory.BinIndexToFrequency(ourSortedIndices[i].Index, SampleLength, SampleRate);
                float theirs = FFTFactory.BinIndexToFrequency(theirSortedIndices[i].Index, SampleLength, SampleRate);
                //Assert.AreEqual(ours, theirs, $"Ours = {GetClosestTestFrequency(ours)} vs Theirs = {GetClosestTestFrequency(theirs)}");
                Debug.Log($"Ours = {GetClosestTestFrequency(ours)} vs Theirs = {GetClosestTestFrequency(theirs)}");
            }

            int maxOurIndex = 0;
            int maxTheirIndex = 0;

            for (var index = 0; index < SampleLength / 2; index++)
            {
                maxOurIndex = ourOutput.Data[index] > ourOutput.Data[maxOurIndex] ? index : maxOurIndex;
                maxTheirIndex = theirOutput[index] > theirOutput[maxTheirIndex] ? index : maxTheirIndex;
            }
        }

        [Test]
        public void SinVsCos()
        {
            Random random = new Random(1);
            float frequency = random.NextFloat(0, SampleLength / 2f);
            float amplitude = random.NextFloat(0f, 1f);
            
            Wave sinWave = new Wave(WaveType.Sine, frequency, 0f, amplitude);
            Samples<float> sinSamples = WaveToFrequencyResponse(sinWave);

            Wave cosWave = new Wave(WaveType.Sine, frequency, math.PI, amplitude);
            Samples<float> cosSamples = WaveToFrequencyResponse(cosWave);

            var unequalComparisons = new NativeList<ComparisonAtIndex<int>>(SampleLength, Allocator.TempJob);
            JobHandle dependency = JobHandle.CombineDependencies(sinSamples.Handle, cosSamples.Handle);
            JobHandle comparisonHandle = new CompareNativeArraysJob<float, int, ComparisonAtIndex<int>> 
            {
                A = sinSamples.DeferredData,
                B = cosSamples.DeferredData,
                Expected = ComparisonEvent.Equal,
                Failures = unequalComparisons.AsParallelWriter()
            }.Schedule(SampleLength, BatchCount, dependency);
            comparisonHandle.Complete();
            unequalComparisons.AsArray().Sort();

            string UnEqualComparisonsReadout()
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (ComparisonAtIndex<int> unequalComparison in unequalComparisons)
                {
                    int index = unequalComparison.AtIndex;
                    stringBuilder.AppendLine($"At position {index}: {sinSamples.Data[index]} vs {cosSamples.Data[index]}");
                }
                return stringBuilder.ToString();
            }

            Assert.AreEqual(unequalComparisons.Length, 0, $"{UnEqualComparisonsReadout()}");
            unequalComparisons.Dispose();
            sinSamples.Dispose();
            cosSamples.Dispose();
        }

        [Test]
        [TestCase(1, 6)]
        public void EqualFrequencyResponse(int numberOfIterations, int numberOfWaves, uint seed = 1)
        {
            Random random = new Random(seed);
            
            for (int iteration = 0; iteration < numberOfIterations; iteration++)
            {
                foreach (WaveType waveType in waveTypes)
                {
                    float frequency = random.NextFloat(0, SampleRate / 2f);
                    float amplitude = random.NextFloat(0f, 1f);
                    Samples<float>[] frequencyResponses = new Samples<float>[numberOfWaves];

                    for (int waveIndex = 0; waveIndex < numberOfWaves; waveIndex++)
                    {
                        float phase = random.NextFloat(0f, 360f);
                        Wave wave = new Wave(waveType, frequency, phase, amplitude);
                        using (new TimeProfiler(readout => Debug.Log(readout.Milliseconds)))
                        {
                            frequencyResponses[waveIndex] = WaveToFrequencyResponse(wave);
                            frequencyResponses[waveIndex].Handle.Complete(); 
                        }
                    }

                    List<JobHandle> comparisonHandles = new List<JobHandle>((int)Combination.nCr(numberOfWaves, 2));
                    var unequalComparisons = new NativeList<ComparisonAtIndex<int>>(SampleLength * numberOfWaves, Allocator.TempJob);

                    for (int waveIndex = 0; waveIndex < numberOfWaves; waveIndex++)
                    {
                        for (int comparisonIndex = waveIndex + 1; comparisonIndex < numberOfWaves; comparisonIndex++)
                        {
                            Samples<float> frequencies = frequencyResponses[waveIndex];
                            Samples<float> frequenciesToCompare = frequencyResponses[comparisonIndex];
                            JobHandle dependency = JobHandle.CombineDependencies(frequencies.Handle, frequenciesToCompare.Handle);
                            JobHandle comparisonJobHandle = new CompareNativeArraysJob<float, int, ComparisonAtIndex<int>> 
                            {
                                    IdentifierA = waveIndex,
                                    IdentifierB = comparisonIndex,
                                    A = frequencies.DeferredData,
                                    B = frequenciesToCompare.DeferredData,
                                    Expected = ComparisonEvent.Equal,
                                    Failures = unequalComparisons.AsParallelWriter()
                            }.Schedule(SampleLength, BatchCount, dependency);
                            comparisonHandles.Add(comparisonJobHandle);
                        }
                    }

                    comparisonHandles.ForEach(handle => handle.Complete());
                    
                    Dictionary<(int a, int b), float> deltas = new Dictionary<(int a, int b), float>();
                    foreach (ComparisonAtIndex<int> comparisonAtIndex in unequalComparisons)
                    {
                        (int a, int b) key = (comparisonAtIndex.IdentifierA, comparisonAtIndex.IdentifierB);
                        float a = frequencyResponses[key.a].Data[comparisonAtIndex.AtIndex];
                        float b = frequencyResponses[key.b].Data[comparisonAtIndex.AtIndex];
                        float delta = math.distance(a, b);
                        deltas[key] = deltas.ContainsKey(key) ? deltas[key] + delta : delta;
                    }

                    string ReadOutComparisonFailures()
                    {
                        StringBuilder stringBuilder = new StringBuilder(unequalComparisons.Length);
                        foreach (var kvp in deltas)
                        {
                            stringBuilder.Append($"\nWave {kvp.Key.a} vs Wave {kvp.Key.b}: {kvp.Value}");
                        }

                        return stringBuilder.ToString();
                    }
                    
                    Assert.AreEqual(unequalComparisons.Length, 0, $"{ReadOutComparisonFailures()}");
                    unequalComparisons.Dispose();
                    foreach (Samples<float> frequencyResponse in frequencyResponses)
                    {
                        frequencyResponse.Dispose();
                    }
                }
            }
        }

        private Samples<float> GetFrequencyResponse(NativeArray<float> data, JobHandle dependency, bool disposeInputData = false)
        {
            Samples<float> samples = new Samples<float>(data, dependency, Allocator.TempJob);
            samples.TransformFromTimeToFrequency(out Samples<Complex> complexFrequencies, Allocator.TempJob);
            complexFrequencies.CollapseToFrequencyBins(out Samples<float> frequencyBins, false, Allocator.TempJob);

            if (disposeInputData)
            {
                samples.Dispose(frequencyBins.Handle);
            }
            complexFrequencies.Dispose(frequencyBins.Handle);

            return frequencyBins;
        }
        
        private Samples<float> WaveToFrequencyResponse(params Wave[] waves)
        {
            NativeArray<float> data = WaveDataFactory.GetRealValueArray(waves,
                                                                    SampleLength,
                                                                    SampleRate,
                                                                    Allocator.TempJob,
                                                                    out JobHandle jobHandle);
            
            Samples<float> samples = new Samples<float>(data, jobHandle, Allocator.TempJob);
            samples.TransformFromTimeToFrequency(out Samples<Complex> complexFrequencies, Allocator.TempJob);
            complexFrequencies.CollapseToFrequencyBins(out Samples<float> frequencyBins, false, Allocator.TempJob);
            
            samples.Dispose(frequencyBins.Handle);
            complexFrequencies.Dispose(frequencyBins.Handle);

            return frequencyBins;
        }

        public override void Setup()
        {
        }

        public override void TearDown()
        {
            FFTFactory.Dispose();
        }
    }
}
