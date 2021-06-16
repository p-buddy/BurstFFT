using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Stopwatch = System.Diagnostics.Stopwatch;

using FFT;

sealed class Test : MonoBehaviour
{
    private const float SampleRate = 44100;
    private const int Width = 1024;
    private const float SampleTime = Width / SampleRate;
    
    public float f1 = 200;
    public float f2 = 500;
    public float f3 = 30;

    public float FreqToCos(float frequency, float percent)
    {
        float totalRotations = frequency * SampleTime;
        float totalAngleRadians = totalRotations * 2f * math.PI;
        return math.cos(percent * totalAngleRadians);
    }
    
    IEnumerable<float> TestData
      => Enumerable.Range(0, Width)
         .Select(i => (float)i / Width)
         .Select(x => FreqToCos(f1, x)
                      + FreqToCos(f2, x)
                      + FreqToCos(f3, x));

    Texture2D Benchmark<TDft>(TDft dft, NativeArray<float> input)
      where TDft : IDft, System.IDisposable
    {
        var texture = new Texture2D(Width / 2, 1, TextureFormat.RFloat, false);
        
        dft.Transform(input);
        
        texture.LoadRawTextureData(dft.Spectrum);
        texture.Apply();

        return texture;
    }
    
    Texture2D _fft;

    private void Start()
    {
        Debug.Log(System.Runtime.InteropServices.Marshal.SizeOf(typeof(float2)));
        Debug.Log(System.Runtime.InteropServices.Marshal.SizeOf(typeof(ComplexBin)));
    }

    void Update()
    {
        using (var data = TempJobMemory.New<float>(TestData))
        {
            using (var ft = new BurstFft(Width))
            {
                _fft  = Benchmark(ft, data);
            }
        }
    }

    void OnGUI()
    {
        if (!Event.current.type.Equals(EventType.Repaint)) return;
        Graphics.DrawTexture(new Rect(10, 64, Width / 2, 16), _fft);
    }
}

