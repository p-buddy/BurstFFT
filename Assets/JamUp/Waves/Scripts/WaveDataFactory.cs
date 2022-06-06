using JamUp.Math;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace JamUp.Waves.Scripts
{
    public static class WaveDataFactory
    {
        private const int InnerLoopBatchCount = 64;

        #region Real
        public static NativeArray<float> GetRealValueArray(in Wave wave, 
                                                           int numberOfSamples, 
                                                           int sampleRate, 
                                                           Allocator allocator, 
                                                           out JobHandle jobHandle)
        {
            NativeArray<float> values = new NativeArray<float>(numberOfSamples, allocator);
            jobHandle = new SetRealValueAtTimeSingleWaveJob
            {
                Data = values,
                Wave = wave,
                SampleRate = sampleRate
            }.Schedule(numberOfSamples, InnerLoopBatchCount);
            return values;
        }

        public static NativeArray<float> GetRealValueArray(Wave[] waves, 
                                                           int numberOfSamples, 
                                                           int sampleRate, 
                                                           Allocator allocator, 
                                                           out JobHandle jobHandle)
        {
            NativeArray<Wave> nativeWaves = new NativeArray<Wave>(waves, allocator);
            NativeArray<float> values = GetRealValueArray(nativeWaves, numberOfSamples, sampleRate, allocator, out jobHandle);
            nativeWaves.Dispose(jobHandle);
            return values;
        }
        
        public static NativeArray<float> GetRealValueArray(NativeArray<Wave> waves, 
                                                           int numberOfSamples, 
                                                           int sampleRate, 
                                                           Allocator allocator, 
                                                           out JobHandle jobHandle)
        {
            NativeArray<float> values = new NativeArray<float>(numberOfSamples, allocator);
            jobHandle = new SetRealValueAtTimeMultipleWavesJob
            {
                Data = values,
                Waves = waves,
                SampleRate = sampleRate
            }.Schedule(numberOfSamples, InnerLoopBatchCount);

            return values;
        }

        public static float GetRealValueAtTime(in Wave wave, float time)
        {
            float rotationAmount = 2f * math.PI * time * wave.Frequency + wave.PhaseOffset;
            switch (wave.WaveType)
            {
                case WaveType.Sine: 
                    return wave.Amplitude * math.sin(rotationAmount);
                case WaveType.Square: 
                    return wave.Amplitude * math.sign(math.sin(rotationAmount));
                case WaveType.Triangle:
                    return wave.Amplitude * 2 / math.PI * math.asin(math.sin(rotationAmount));
                case WaveType.Sawtooth:
                    return wave.Amplitude * -2f / math.PI * math.atan(1f / math.tan(rotationAmount - math.PI / 2f));
                default:
                    return 0f;
            }
        }
        #endregion Real 

        #region Complex
        public static NativeArray<Complex> GetComplexValueArray(in Wave wave, 
                                                                int length, 
                                                                int sampleRate, 
                                                                Allocator allocator, 
                                                                out JobHandle jobHandle)
        {
            NativeArray<Complex> values = new NativeArray<Complex>(length, allocator);
            jobHandle = new SetComplexValueAtTimeSingleWaveJob
            {
                Data = values,
                Wave = wave,
                SampleRate = sampleRate
            }.Schedule(length, InnerLoopBatchCount);

            return values;
        }
        
        public static NativeArray<Complex> GetComplexValueArray(NativeArray<Wave> waves, 
                                                                int length, 
                                                                int sampleRate, 
                                                                Allocator allocator, 
                                                                out JobHandle jobHandle)
        {
            NativeArray<Complex> values = new NativeArray<Complex>(length, allocator);
            jobHandle = new SetComplexValueAtTimeMultipleWavesJob
            {
                Data = values,
                Waves = waves,
                SampleRate = sampleRate
            }.Schedule(length, InnerLoopBatchCount);

            return values;
        }
        
        public static Complex GetComplexValueAtTime(in Wave wave, float time)
        {
            Wave cosWave = Wave.SinToCos(in wave);
            return new Complex(GetRealValueAtTime(in cosWave, time), GetRealValueAtTime(in wave, time));
        }
        #endregion Complex
        
        #region Jobs
        [BurstCompile]
        private struct SetRealValueAtTimeSingleWaveJob : IJobParallelFor
        {
            [ReadOnly]
            public int SampleRate;

            [ReadOnly] 
            public Wave Wave;

            public NativeArray<float> Data;

            public void Execute(int index)
            {
                float time = index * 1f / SampleRate;
                Data[index] += GetRealValueAtTime(in Wave, time);
            }
        }
        
        
        [BurstCompile]
        private struct SetRealValueAtTimeMultipleWavesJob : IJobParallelFor
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
                    Data[index] += GetRealValueAtTime(in wave, time);
                }
            }
        }
        
        [BurstCompile]
        private struct SetComplexValueAtTimeSingleWaveJob : IJobParallelFor
        {
            [ReadOnly]
            public int SampleRate;

            [ReadOnly] 
            public Wave Wave;

            public NativeArray<Complex> Data;

            public void Execute(int index)
            {
                float time = index * 1f / SampleRate;
                Data[index] += GetComplexValueAtTime(in Wave, time);
            }
        }
        
        [BurstCompile]
        private struct SetComplexValueAtTimeMultipleWavesJob : IJobParallelFor
        {
            [ReadOnly]
            public int SampleRate;

            [ReadOnly] 
            public NativeArray<Wave> Waves;

            public NativeArray<Complex> Data;

            public void Execute(int index)
            {
                foreach (Wave wave in Waves)
                {
                    float time = index * 1f / SampleRate;
                    Data[index] += GetComplexValueAtTime(in wave, time);
                }
            }
        }
        #endregion Jobs
    }
}