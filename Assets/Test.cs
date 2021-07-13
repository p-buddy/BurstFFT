using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Stopwatch = System.Diagnostics.Stopwatch;

using FFT;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

sealed class Test : MonoBehaviour
{
    private const float SampleRate = 44100;
    private const int Width = 1024;
    private const float SampleTime = Width / SampleRate;
    
    public float f1 = 200;
    public float f2 = 500;
    public float f3 = 30;
    
    private Texture2D inputTexture;
    private Texture2D forwardTexture;
    private Texture2D inverseTexture;

    public float FreqToCos(float frequency, float percent, float phase)
    {
        float totalRotations = frequency * SampleTime;
        float totalAngleRadians = totalRotations * 2f * math.PI;
        return math.cos(percent * totalAngleRadians + phase);
    }
    
    IEnumerable<float> TestData
      => Enumerable.Range(0, Width)
         .Select(i => (float)i / Width)
         .Select(x => FreqToCos(f1, x, math.radians(30f))
                      + FreqToCos(f2, x, math.radians(217f))
                      + FreqToCos(f3, x, math.radians(313f)));

    void PerformTest<TDft>(TDft dft, NativeArray<float> input)
      where TDft : IDft, System.IDisposable
    {
        inputTexture = new Texture2D(Width / 2, 1, TextureFormat.RFloat, false);
        inputTexture.LoadRawTextureData(input);
        inputTexture.Apply();

        forwardTexture = new Texture2D(Width / 2, 1, TextureFormat.RFloat, false);
        FFTOutput<ComplexBin> bins = FFTFactory.TransformToBins(new FFTInput<float>(input));
        FFTOutput<float> spectrum = bins.ToSamples(false);
        spectrum.Handle.Complete();
        forwardTexture.LoadRawTextureData(spectrum.Data);
        forwardTexture.Apply();
        spectrum.Data.Dispose();
        
        inverseTexture = new Texture2D(Width / 2, 1, TextureFormat.RFloat, false);
        FFTOutput<float> samples = FFTFactory.InverseTransformToFloats(new FFTInput<ComplexBin>(bins.Data));
        samples.Handle.Complete();
        inverseTexture.LoadRawTextureData(samples.Data);
        inverseTexture.Apply();
        bins.Data.Dispose();
        samples.Data.Dispose();
        
        var texture = new Texture2D(Width / 2, 1, TextureFormat.RFloat, false);
        dft.Transform(input);
        texture.LoadRawTextureData(dft.Spectrum);
        texture.Apply();
    }

    private void Start()
    {
        
    }

    void Update()
    {
        using (var data = TempJobMemory.New(TestData))
        {
            using (var ft = new BurstFft(Width))
            {
                PerformTest(ft, data);
            }
        }
    }

    void OnGUI()
    {
        if (!Event.current.type.Equals(EventType.Repaint)) return;
        Graphics.DrawTexture(new Rect(10, 50, Width / 2, 16), inputTexture);
        Graphics.DrawTexture(new Rect(10, 200, Width / 2, 16), forwardTexture);
        Graphics.DrawTexture(new Rect(10, 350, Width / 2, 16), inverseTexture);
    }
}

