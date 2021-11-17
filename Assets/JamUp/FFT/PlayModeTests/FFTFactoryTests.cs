using System;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using UnityEngine;
using UnityEngine.TestTools;

using JamUp.DataVisualization;
using JamUp.Math;
using JamUp.Waves;

namespace JamUp.FFT.PlayModeTests
{
    public class FFTFactoryTests
    {
        public const int SampleRate = 44100;
        public const int SampleLength = 1024;
        
        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void TearDown()
        {
            DrawAsTexture.Dispose();
        }
        
        public readonly struct TestCase
        {
            public Wave[] Waves { get; }
            public float TestTime { get; }

            public TestCase(float testTime, params Wave[] waves)
            {
                TestTime = testTime;
                Waves = waves;
            }
            
            public TestCase(params Wave[] waves)
            {
                TestTime = default;
                Waves = waves;
            }
            
            public override string ToString()
            {
                return String.Join("; ", Waves);
            }
        }
        
        public static List<TestCase> StaticWaves = new List<TestCase>
        {
            new TestCase(3f, new Wave(WaveType.Sine, 10f)),
            new TestCase(3f, new Wave(WaveType.Sine, 10f, math.radians(45f))),
            new TestCase(3f, new Wave(WaveType.Triangle, 50f)),
            new TestCase(3f, new Wave(WaveType.Sawtooth, 50f)),
            new TestCase(3f, new Wave(WaveType.Square, 1f)),

        };
        
        [UnityTest]
        public IEnumerator StaticFrequencies([ValueSource(nameof(StaticWaves))]TestCase testCase)
        {
            Wave[] waves = testCase.Waves;
            float time = Time.time;

            while (Time.time <= time + testCase.TestTime)
            {
                NativeArray<float> data = WaveDataFactory.GetRealValueArray(waves,
                                                                        SampleLength,
                                                                        SampleRate,
                                                                        Allocator.TempJob,
                                                                        out JobHandle jobHandle);
                Samples<float> samples = new Samples<float>(data, jobHandle, Allocator.TempJob);
                yield return PerformFFTAndDraw(samples);
            }
        }

        public static List<TestCase> MovingWaves = new List<TestCase>
        {
            new TestCase(new Wave(WaveType.Sine, 0f), new Wave(WaveType.Sine, 22050f)),
            new TestCase(new Wave(WaveType.Sine, 50f), new Wave(WaveType.Sine, 1f)),
            new TestCase(new Wave(WaveType.Sine, 30f),
                         new Wave(WaveType.Sine, 5000f),
                         new Wave(WaveType.Sine, 2000f),
                         new Wave(WaveType.Sine, 3000f),
                         new Wave(WaveType.Sine, 4f),
                         new Wave(WaveType.Sine, 40f)),
        };
        
        [UnityTest]
        public IEnumerator MovingFrequencies([ValueSource(nameof(MovingWaves))]TestCase testCase)
        {
            Assert.IsTrue(testCase.Waves.Length % 2 == 0);

            int halfLength = testCase.Waves.Length / 2;
            IList<Wave> start = new ArraySegment<Wave>(testCase.Waves, 0, halfLength);
            IList<Wave> end = new ArraySegment<Wave>(testCase.Waves, halfLength, halfLength);;
            
            for (int i = 0; i < halfLength; i++)
            {
                Assert.AreEqual(start[i].WaveType, end[i].WaveType);
            }
            
            const int numSteps = 1000;
            const float stepSize = (float) 1 / numSteps;
            float ratio = 0f;
            
            while (ratio <= 1.0f)
            {
                Wave[] currentWaves = new Wave[halfLength];
                for (int i = 0; i < halfLength; i++)
                {
                    currentWaves[i] = Wave.Lerp(start[i], end[i], ratio);
                }

                NativeArray<float> data = WaveDataFactory.GetRealValueArray(currentWaves,
                                                                       SampleLength,
                                                                       SampleRate,
                                                                       Allocator.TempJob,
                                                                       out JobHandle jobHandle);
                
                Samples<float> samples = new Samples<float>(data, jobHandle, Allocator.TempJob);
                yield return PerformFFTAndDraw(samples);
                ratio += stepSize;
            }
        }

        private IEnumerator PerformFFTAndDraw(Samples<float> samples)
        {
            samples.TransformFromTimeToFrequency(out Samples<Complex> complexFrequencies, Allocator.TempJob);
            complexFrequencies.CollapseToFrequencyBins(out Samples<float> frequencyBins, false, Allocator.TempJob);
            complexFrequencies.InverseTransformFromFrequencyToTime(out Samples<float> backToTime, Allocator.TempJob, frequencyBins.Handle);
                
            DrawAsTexture.Draw(samples.Data);
            DrawAsTexture.Draw(frequencyBins.Data.GetSubArray(0, frequencyBins.Data.Length / 2));
            DrawAsTexture.Draw(backToTime.Data);
                
            samples.Dispose();
            complexFrequencies.Dispose();
            frequencyBins.Dispose();
            backToTime.Dispose();
            
            yield return null;
            yield return null;
            DrawAsTexture.Clear();
        }
    }
}