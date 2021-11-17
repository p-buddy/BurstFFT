using System;
using System.Collections.Generic;
using System.Linq;
using JamUp.Math;
using JamUp.Waves;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace JamUp.DataVisualization.Waves
{
    [Serializable]
    public struct WaveDrawingState 
    {
        public enum DrawingMode
        {
            Real,
            RealAsComplexComponent,
            Complex,
            SignalAroundSignal,
        }

        [SerializeField]
        public SerializableWave[] serializableWaves;

        [SerializeField]
        [Range(1, 44100)]
        public int sampleRate;

        [SerializeField]
        [Range(0.1f, 10f)]
        public float time;

        [SerializeField]
        [Range(float.Epsilon, 1f)]
        public float thickness;


        [SerializeField]
        public Color color;


        [SerializeField]
        public DrawingMode mode;

        private NativeArray<Wave> GetWaves(Allocator allocator = Allocator.TempJob) =>
            new NativeArray<Wave>(serializableWaves.Select(serial => serial.Wave).ToArray(), allocator);

        public Color Color => color;

        public bool StateRequiresMeshRebuild(WaveDrawingState other)
        {
            return !(serializableWaves != null && other.serializableWaves != null &&
                     serializableWaves.Length == other.serializableWaves.Length &&
                     WavesMatch(other) &&
                     mode == other.mode &&
                     sampleRate == other.sampleRate &&
                     time == other.time &&
                     thickness == other.thickness);
        }

        private bool WavesMatch(WaveDrawingState other)
        {
            for (var index = 0; index < serializableWaves.Length; index++)
            {
                if (serializableWaves[index] != other.serializableWaves[index])
                {
                    return false;
                }
            }

            return true;
        }

        public MeshData MeshDataForState()
        {
            NativeArray<Wave> waves = GetWaves();
            int length = (int)(sampleRate * time);

            MeshData meshData;
            switch (mode)
            {
                case DrawingMode.Real:
                    NativeArray<float> realData = WaveDataFactory.GetRealValueArray(waves, length, sampleRate, Allocator.TempJob, out JobHandle realHandle);
                    var realToPositionConverter = new RealSampleToWorldPoint(sampleRate, default, math.forward());
                    meshData = DrawAsMesh.ConstructMesh(realData, in realToPositionConverter, thickness, realHandle);
                    realData.Dispose(meshData.CombinedHandle);
                    break;
                case DrawingMode.Complex:
                    NativeArray<Complex> complexData = WaveDataFactory.GetComplexValueArray(waves, length, sampleRate, Allocator.TempJob, out JobHandle complexHandle);
                    var realToComplexConverter = new ComplexSampleToWorldPoint(sampleRate);
                    meshData = DrawAsMesh.ConstructMesh(complexData, in realToComplexConverter, thickness, complexHandle);
                    complexData.Dispose(meshData.CombinedHandle);
                    break;
                default:
                    return default;
            }
            
            waves.Dispose(meshData.CombinedHandle);
            return meshData;
        }

        public WaveDrawingState Copy()
        {
            return new WaveDrawingState
            {
                mode = mode,
                color = color,
                sampleRate = sampleRate,
                serializableWaves = serializableWaves.ToArray(),
                thickness = thickness,
                time = time
            };
        }

        #region Constructors

        public static WaveDrawingState ForReal(int sampleRate, float thickness, float time, Color color, params Wave[] waves)
        {
            return new WaveDrawingState
            {
                sampleRate = sampleRate,
                thickness = thickness,
                time = time,
                color = color,
                serializableWaves = waves.Select(wave => (SerializableWave)wave).ToArray(),
                mode = DrawingMode.Real
            };
        }
        #endregion
    }
}