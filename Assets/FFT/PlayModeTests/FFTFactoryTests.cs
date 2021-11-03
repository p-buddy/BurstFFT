using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

using NUnit.Framework;
using UnityEngine.TestTools;

namespace FFT.PlayModeTests
{
    public class FFTFactoryTests
    {
        public readonly struct TestCase
        {
            private const int Length = 1024;
            private const float SampleRate = 44100;
            private const float SampleTime = Length / SampleRate;
            private readonly NativeArray<float> data;

            public NativeArray<float> Data => data;
            public float[] Frequencies { get; }
            
            public TestCase(params float[] frequencies)
            {
                Frequencies = frequencies;
                data = new NativeArray<float>(Length, Allocator.Persistent);
                Random random = new Random(1);

                foreach (float frequency in frequencies)
                {
                    float phase = math.radians(random.NextFloat(0f, 360f));
                    float[] toAdd = DataForFrequency(frequency, phase);
                    for (var index = 0; index < toAdd.Length; index++)
                    {
                        data[index] = data[index] + toAdd[index];
                    }
                }
            }

            private static float[] DataForFrequency(float frequency, float phase)
            {
                return Enumerable.Range(0, Length)
                                 .Select(index => (float) index / Length)
                                 .Select(percent => ValueFromFrequency(frequency, percent, phase))
                                 .ToArray();
            }
            
            private static float ValueFromFrequency(float frequency, float percent, float phase)
            {
                float totalRotations = frequency * SampleTime;
                float totalAngleRadians = totalRotations * 2f * math.PI;
                return math.cos(percent * totalAngleRadians + phase);
            }

            public static TestCase Lerp(TestCase a, TestCase b, float s)
            {
                Assert.IsTrue(a.Frequencies.Length == b.Frequencies.Length);
                float[] lerpedFrequencies = new float[a.Frequencies.Length];
                for (var index = 0; index < a.Frequencies.Length; index++)
                {
                    lerpedFrequencies[index] = math.lerp(a.Frequencies[index], b.Frequencies[index], s);
                }
                return new TestCase(lerpedFrequencies);
            }
        }
        
        [TestCase(new []{0f}, new []{22050f}, ExpectedResult = null)]
        [TestCase(new []{30f, 5000f, 2000f}, new []{3000f, 4f, 40f}, ExpectedResult = null)]
        [UnityTest]
        public IEnumerator MovingFrequencies(float[] startFrequencies, float[] endFrequencies)
        {
            TestCase start = new TestCase(startFrequencies);
            TestCase end = new TestCase(endFrequencies);
            TextureDrawer textureDrawer = new GameObject().AddComponent<TextureDrawer>();

            const int numSteps = 1000;
            const float stepSize = (float) 1 / numSteps;
            float ratio = 0f;
            
            while (ratio <= 1.0f)
            {
                TestCase testCase = TestCase.Lerp(start, end, ratio);
                Samples<float> samples = new Samples<float>(testCase.Data);
                
                samples.TransformFromTimeToFrequency(out Samples<Complex> complexFrequencies, Allocator.TempJob);
                complexFrequencies.CollapseToFrequencyBins(out Samples<float> frequencyBins, false, Allocator.TempJob);
                complexFrequencies.InverseTransformFromFrequencyToTime(out Samples<float> backToTime, Allocator.TempJob, frequencyBins.Handle);
                
                textureDrawer.DrawNativeArrayAsTexture(testCase.Data);
                textureDrawer.DrawNativeArrayAsTexture(frequencyBins.Data);
                textureDrawer.DrawNativeArrayAsTexture(backToTime.Data);
                
                samples.Dispose();
                complexFrequencies.Dispose();
                frequencyBins.Dispose();
                backToTime.Dispose();
                
                yield return null;
                yield return null;
                ratio += stepSize;
                textureDrawer.Clear();
            }

            Object.DestroyImmediate(textureDrawer.gameObject);
            //FFTFactory.Dispose();
        }
        

        private class TextureDrawer : MonoBehaviour
        {
            private readonly Queue<Texture2D> texturePool = new Queue<Texture2D>();
            private readonly List<Texture2D> textures = new List<Texture2D>();
            
            public float Height { get; set; } = 16f;
            public float Width { get; set; } = 1000f;
            public float VerticalPadding { get; set; } = 100f;
            public float HorizontalPadding { get; set; } = 10f;

            public void DrawNativeArrayAsTexture(NativeArray<float> data)
            {
                Texture2D texture;
                if (texturePool.Count == 0)
                {
                    texture = new Texture2D(data.Length / 2, 1, TextureFormat.RFloat, false);
                }
                else
                {
                    texture = texturePool.Dequeue();
                }
                texture.LoadRawTextureData(data);
                texture.Apply();
                textures.Add(texture);
            }

            public void Clear()
            {
                foreach (Texture2D texture in textures)
                {
                    texturePool.Enqueue(texture);
                }
                textures.Clear();
            }
            
            private void OnGUI()
            {
                if (!Event.current.type.Equals(EventType.Repaint))
                {
                    return;
                }

                for (var index = 0; index < textures.Count; index++)
                {
                    Texture texture = textures[index];
                    var rect = new Rect(HorizontalPadding, VerticalPadding * (index + 1), Width, Height);
                    Graphics.DrawTexture(rect, texture);
                }
            }
        }
    }
}