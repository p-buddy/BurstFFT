using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace JamUp.Waves
{
    public static class WaveDataFactory
    {
        private const int InnerLoopBatchCount = 64;

        public static NativeArray<float> GetValueArray(in Wave wave,
                                                       int length,
                                                       int sampleRate,
                                                       Allocator allocator, 
                                                       out JobHandle jobHandle)
        {
            NativeArray<float> values = new NativeArray<float>(length, allocator);
            jobHandle = new SetValueAtTimeSingleWaveJob
            {
                Data = values,
                Wave = wave,
                SampleRate = sampleRate
            }.Schedule(length, InnerLoopBatchCount);
            return values;
        }

        public static NativeArray<float> GetValueArray(Wave[] waves,
                                                       int length,
                                                       int sampleRate,
                                                       Allocator allocator, 
                                                       out JobHandle jobHandle)
        {
            NativeArray<Wave> nativeWaves = new NativeArray<Wave>(waves, allocator);
            NativeArray<float> values = GetValueArray(nativeWaves, length, sampleRate, allocator, out jobHandle);
            nativeWaves.Dispose(jobHandle);
            return values;
        }
        
        public static NativeArray<float> GetValueArray(NativeArray<Wave> waves,
                                                       int length,
                                                       int sampleRate,
                                                       Allocator allocator, 
                                                       out JobHandle jobHandle)
        {
            NativeArray<float> values = new NativeArray<float>(length, allocator);
            jobHandle = new SetValueAtTimeMultipleWavesJob
            {
                Data = values,
                Waves = waves,
                SampleRate = sampleRate
            }.Schedule(length, InnerLoopBatchCount);

            return values;
        }

        public static float GetValueAtTime(in Wave wave, float time)
        {
            float rotationAmount = 2f * math.PI * time * wave.Frequency + wave.PhaseOffset;
            switch (wave.WaveType)
            {
                case WaveType.Sine: 
                    return wave.Amplitude * math.sin(rotationAmount);
                case WaveType.Square: 
                    return math.sign(math.sin(rotationAmount));
                case WaveType.Triangle:
                    return 1f - 4f * math.abs(math.round(rotationAmount - 0.25f) - (rotationAmount - 0.25f));
                case WaveType.Sawtooth:
                    return 2f * (rotationAmount - math.floor(rotationAmount + 0.5f));
                default:
                    return 0f;
            }
        }

        [BurstCompile]
        private struct SetValueAtTimeSingleWaveJob : IJobParallelFor
        {
            [ReadOnly]
            public int SampleRate;

            [ReadOnly] 
            public Wave Wave;

            public NativeArray<float> Data;

            public void Execute(int index)
            {
                float time = index * 1f / SampleRate;
                Data[index] += GetValueAtTime(in Wave, time);
            }
        }
        
        
        [BurstCompile]
        private struct SetValueAtTimeMultipleWavesJob : IJobParallelFor
        {
            [ReadOnly]
            public int SampleRate;

            [ReadOnly] 
            public NativeArray<Wave> Waves;

            public NativeArray<float> Data;

            public void Execute(int index)
            {
                foreach (Wave wave in Waves)
                {
                    float time = index * 1f / SampleRate;
                    Data[index] += GetValueAtTime(in wave, time);
                }
            }
        }
    }
}