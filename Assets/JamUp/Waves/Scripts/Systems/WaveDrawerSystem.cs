using System;
using System.Collections.Generic;
using JamUp.UnityUtility;
using JamUp.Waves.Scripts.API;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace JamUp.Waves.Scripts
{
    public partial class WaveDrawerSystem: SystemBase
    {
        private const int MaxWaveCount = 10;
        private const float Attack = 0.01f;
        private const float Release = 0.01f;

        private int settingIndex;
        
        private Material material;
        
        private ConstantShaderPropertyWrapper<int> WaveCount_StartTime_EndTime;
        private ConstantShaderPropertyWrapper<float> StartTime;
        private ConstantShaderPropertyWrapper<float> EndTime;

        private Dictionary<ShaderVariable, IShaderPropertyWrapper> nonWaveShaderProperties;

        private struct ShaderPropertiesBlittable
        {
            public ShaderProperty<int> WaveCount;
            public ShaderProperty<float> StartTime;
            public ShaderProperty<float> EndTime;

            public AnimatableShaderProperty<int> SampleRate;
            public AnimatableShaderProperty<float> Thickness;
            public AnimatableShaderProperty<float> SignalTime;

            public void Apply(MaterialPropertyBlock block)
            {
                block.Set(WaveCount);
                block.Set(StartTime);
                block.Set(EndTime);
                block.Set(SampleRate);
                block.Set(Thickness);
                block.Set(SignalTime);
            }
        }

        private NativeArray<ShaderPropertiesBlittable> currentBlittableSettings;
        
        private NativeArray<float4x4> nativeCurrentWaves;
        private ShaderProperty<Matrix4x4[]> currentWavesSetting;

        private NativeArray<int> numberOfVertices;

        private EntityArchetype archetype;
        private EndSimulationEntityCommandBufferSystem endSimulationEcbSystem;
        private float2 currentTimeFrame;

        private Bounds bounds;
        private MaterialPropertyBlock propertyBlock;


        protected override void OnCreate()
        {
            base.OnCreate();
            archetype = EntityManager.CreateArchetype(typeof(TransitionState));
            endSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            Transform transform = new GameObject().transform;
            
            propertyBlock = new MaterialPropertyBlock();
            propertyBlock.Set(new ShaderProperty<UnityEngine.Matrix4x4>("WaveOriginToWorldMatrix")
                                  .WithValue(transform.localToWorldMatrix));
            propertyBlock.Set(new ShaderProperty<UnityEngine.Matrix4x4>("WorldToWaveOriginMatrix")
                                  .WithValue(transform.worldToLocalMatrix));
            
            Object.Destroy(transform.gameObject);

            currentBlittableSettings = new NativeArray<ShaderPropertiesBlittable>(1, Allocator.Persistent);
            currentBlittableSettings[0] = new ()
            {   
                WaveCount = new (nameof(ShaderPropertiesBlittable.WaveCount)),
                StartTime = new (nameof(ShaderPropertiesBlittable.StartTime)),
                EndTime = new (nameof(ShaderPropertiesBlittable.EndTime)),
                SampleRate = new (nameof(ShaderPropertiesBlittable.SampleRate)),
                SignalTime = new (nameof(ShaderPropertiesBlittable.SignalTime)),
                Thickness = new (nameof(ShaderPropertiesBlittable.Thickness)),
            };

            nativeCurrentWaves = new NativeArray<float4x4>(MaxWaveCount, Allocator.Persistent);
            currentWavesSetting = new ShaderProperty<Matrix4x4[]>("WaveTransitionData").WithValue(new Matrix4x4[MaxWaveCount]);
            numberOfVertices = new NativeArray<int>(1, Allocator.Persistent);
            bounds = new Bounds(Vector3.zero, Vector3.one * 50f);
        }

        protected override void OnUpdate()
        {
            if (currentTimeFrame.y < (float)Time.ElapsedTime)
            {
                // perform job to update current settings and waves
                
                nativeCurrentWaves.Reinterpret<Matrix4x4>().CopyTo(currentWavesSetting.Value);
                propertyBlock.Set(currentWavesSetting);
                
                currentBlittableSettings[0].Apply(propertyBlock);
                
                // Put into job
                ShaderPropertiesBlittable current = currentBlittableSettings[0];
                float maxSignalTime = Mathf.Max(current.SignalTime.From.Value, current.SignalTime.To.Value);
                int maxSampleRate = System.Math.Max(current.SampleRate.From.Value, current.SampleRate.To.Value);
                numberOfVertices[0] = 24 * (int)(maxSignalTime / (1f / maxSampleRate));
            }

            Graphics.DrawProcedural(material, bounds, MeshTopology.Triangles, numberOfVertices[0], 0, null, propertyBlock, ShadowCastingMode.TwoSided);
        }
        
        

        public KeyFrame GetCurrent(bool dispose = true)
        {
            float time = UnityEngine.Time.timeSinceLevelLoad;
            var props = nonWaveShaderProperties;
            float startTime = props[ShaderVariable.StartTime].Constant<float>();
            float duration = props[ShaderVariable.EndTime].Constant<float>() - startTime;
            float lerpTime = (time - startTime) / duration;

            return new KeyFrame(Attack,
                                props[ShaderVariable.SampleRate].Animated<float>(lerpTime, Mathf.Lerp).Value,
                                props[ShaderVariable.Projection].Animated<float>(lerpTime, Mathf.Lerp).Value,
                                props[ShaderVariable.Thickness].Animated<float>(lerpTime, Mathf.Lerp).Value,
                                null,
                                props[ShaderVariable.SignalLength].Animated<float>(lerpTime, Mathf.Lerp).Value
                                );
        }
        
        public void CreateWaveAnimation(in Signal signal)
        {
            float now = UnityEngine.Time.timeSinceLevelLoad;
            int frameCount = signal.Frames.Count;
            int paddedFrameCount = 1 + frameCount + 1;
            
            KeyFrame.JobFriendlyRepresentation capture = new KeyFrame.JobFriendlyRepresentation
            {
                Projections = new NativeArray<AnimatableProperty<float>>(paddedFrameCount, Allocator.Persistent),
                SampleRates = new NativeArray<AnimatableProperty<float>>(paddedFrameCount, Allocator.Persistent),
                SignalLengths = new NativeArray<AnimatableProperty<float>>(paddedFrameCount, Allocator.Persistent),
                Thicknesses = new NativeArray<AnimatableProperty<float>>(paddedFrameCount, Allocator.Persistent),
                Durations = new NativeArray<float>(paddedFrameCount, Allocator.TempJob),
                WaveCounts = new NativeArray<int>(paddedFrameCount, Allocator.Persistent)
            };

            GetCurrent().CaptureForJob(capture, 0);
            for (int index = 0; index < frameCount; index++)
            {
                signal.Frames[index].CaptureForJob(capture, index + 1);
            }
            signal.Frames[frameCount].CaptureForJob(capture, frameCount);
            
            NativeArray<int> runningTotalWaveCounts = new (frameCount, Allocator.Persistent);
            
            // Need to account from current wavestate setting, and the 'release' wave
            JobHandle waveCountTotalsJob = new CalculateRunningTotalJob.Int
            {
                Input = capture.WaveCounts,
                RunningTotal = runningTotalWaveCounts
            }.Schedule();

            NativeArray<float> times = new (paddedFrameCount + 1, Allocator.Persistent);
            times[0] = now;
            times[1] = now + Attack;
            JobHandle collectTimesJob = new CollectTimesBasedOnDurations
            {
                LastDuration = Release,
                Durations = capture.Durations,
                Times = times
            }.Schedule();
            capture.Durations.Dispose(collectTimesJob);

            nonWaveShaderProperties = new Dictionary<ShaderVariable, IShaderPropertyWrapper>(new[]
            {
                MakeConstantVariablePair(ShaderVariable.WaveCount, capture.WaveCounts),
                MakeConstantVariablePair(ShaderVariable.StartTime, times),
                MakeConstantVariablePair(ShaderVariable.EndTime, times, 1),
                MakeAnimatedVariablePair(ShaderVariable.Projection, capture.Projections),
                MakeAnimatedVariablePair(ShaderVariable.SignalLength, capture.SignalLengths),
                MakeAnimatedVariablePair(ShaderVariable.Thickness, capture.Thicknesses),
            });

            waveCountTotalsJob.Complete();
            int totalWaves = runningTotalWaveCounts[frameCount - 1];
            NativeArray<AnimatableProperty<WaveState>> waves = new (totalWaves, Allocator.Persistent);
            
            for (int index = 1; index < signal.Frames.Count; index++)
            {
                signal.Frames[index].CaptureWaves(waves, runningTotalWaveCounts[index - 1]);
            }
            
            collectTimesJob.Complete();
        }

        private KeyValuePair<ShaderVariable, IShaderPropertyWrapper> MakeConstantVariablePair<T>(
            ShaderVariable variable,
            NativeArray<T> allSettings,
            int offset = 0) where T : struct =>
            new(variable, new ConstantShaderPropertyWrapper<T>(allSettings, variable.Name(), offset));
        
        private KeyValuePair<ShaderVariable, IShaderPropertyWrapper> MakeAnimatedVariablePair<T>(
            ShaderVariable variable,
            NativeArray<AnimatableProperty<T>> allSettings) where T : struct =>
            new(variable, new AnimatedShaderPropertyWrapper<T>(allSettings, variable.Name()));
    }
}