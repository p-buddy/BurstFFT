using JamUp.UnityUtility;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.Waves.Scripts
{
    public struct WaveShaderProperties
    {
        public AnimatedShaderPropertyWrapper<float> Projection { get; }
        public AnimatedShaderPropertyWrapper<float> SampleRate { get; }
        public AnimatedShaderPropertyWrapper<float> Thickness { get; }
        public AnimatedShaderPropertyWrapper<float> SignalLength { get; }
        public ConstantShaderPropertyWrapper<int> WaveCount { get; }
        public ConstantShaderPropertyWrapper<float> StartTime { get; }
        public ConstantShaderPropertyWrapper<float> EndTime { get; }
        public NativeArray<int> VertexCount { get; private set; }
        public int CurrentIndex { get; private set; }
        public int Length { get; }

        public WaveShaderProperties(
            NativeArray<AnimatableProperty<float>> projections, 
            NativeArray<AnimatableProperty<float>> sampleRates,
            NativeArray<AnimatableProperty<float>> thicknesses,
            NativeArray<AnimatableProperty<float>> signalLengths,
            NativeArray<int> waveCounts,
            NativeArray<float> times)
        {
            CurrentIndex = 0;
            Projection = new AnimatedShaderPropertyWrapper<float>(projections, nameof(Projection));
            SampleRate = new AnimatedShaderPropertyWrapper<float>(sampleRates, nameof(SampleRate));
            Thickness = new AnimatedShaderPropertyWrapper<float>(thicknesses, nameof(Thickness));
            SignalLength = new AnimatedShaderPropertyWrapper<float>(signalLengths, nameof(SignalLength));
            WaveCount = new ConstantShaderPropertyWrapper<int>(waveCounts, nameof(WaveCount));
            StartTime = new ConstantShaderPropertyWrapper<float>(times, nameof(StartTime));
            EndTime = new ConstantShaderPropertyWrapper<float>(times, nameof(EndTime), 1);
            VertexCount = new NativeArray<int>(0, Allocator.Persistent);
            new SetVertexCount
            {
                SignalTime = SignalLength.CurrentSetting,
                SampleRate = SampleRate.CurrentSetting,
                VertexCount = VertexCount
            }.Run();
            Length = projections.Length;
            Assert.AreEqual(Length, sampleRates.Length);
            Assert.AreEqual(Length, sampleRates.Length);
            Assert.AreEqual(Length, thicknesses.Length);
            Assert.AreEqual(Length, signalLengths.Length);
            Assert.AreEqual(Length, waveCounts.Length);
            Assert.AreEqual(Length, times.Length - 1); // hmm?
        }

        public bool TryUpdate(out JobHandle jobHandle)
        {
            if (EndTime.CurrentSetting[0].Value < Time.timeSinceLevelLoad)
            {
                jobHandle = default;
                return false;
            }
            
            CurrentIndex++;
            var index = CurrentIndex;
            bool isLast = index == Length - 1; // or minus 2?
            JobHandle sampleRate = SampleRate.Update(index, isLast);
            JobHandle signalLength = Thickness.Update(index, isLast);
            
            JobHandle waveCounts = default; // needs custom
            JobHandle combined1 = JobHandle.CombineDependencies(Projection.Update(index, isLast),
                                                                Thickness.Update(index, isLast),
                                                                waveCounts);
            
            JobHandle vertexCount = new SetVertexCount()
            {
                SignalTime = SignalLength.CurrentSetting,
                SampleRate = SampleRate.CurrentSetting,
                VertexCount = VertexCount
            }.Schedule(JobHandle.CombineDependencies(signalLength, sampleRate));

            JobHandle combined2 = JobHandle.CombineDependencies(StartTime.Update(index), EndTime.Update(index), vertexCount);
            
            jobHandle = JobHandle.CombineDependencies(combined1, combined2);
            return true;
        }
    }
}