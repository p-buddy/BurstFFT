using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JamUp.UnityUtility;
using JamUp.Waves.Scripts.API;
using pbuddy.TypeScriptingUtility.RuntimeScripts;
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
        
        private Dictionary<int, MaterialPropertyBlock> propertyBlocks;

        private AnimatableShaderProperty<float> projectionShaderProperty;
        private AnimatableShaderProperty<float> thicknessShaderProperty;
        private AnimatableShaderProperty<float> signalLengthShaderProperty;
        private AnimatableShaderProperty<float> sampleRateShaderProperty;
        protected override void OnCreate()
        {
            base.OnCreate();
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
            Entities.WithoutBurst().ForEach((in PropertyBlockReference propertyBlockRef, in VertexCount vertexCount) =>
            {
                var propertyBlock = propertyBlocks[propertyBlockRef.ID];
                int count = vertexCount.Value;
                MeshTopology topology = MeshTopology.Triangles;
                ShadowCastingMode shadow = ShadowCastingMode.TwoSided;
                Graphics.DrawProcedural(material, bounds, topology, count, 0, null, propertyBlock, shadow);
            }).Run();
            
            float time = UnityEngine.Time.timeSinceLevelLoad;
            var ecb = endSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
            
            JobHandle determineTransitions = Entities.WithNone<Step>()
                    .ForEach((Entity entity,
                              int index,
                              ref CurrentTimeFrame current,
                              ref DynamicBuffer<DurationElement> elements) =>
                    {
                        if (!current.UpdateRequired(time, Time.DeltaTime)) return;
                        elements.RemoveAt(0);
                        DurationElement duration = elements[0];
                        current.StartTime = time;
                        current.EndTime = time + duration;
                        ecb.AddComponent<Step>(index, entity);
                    }).ScheduleParallel(Dependency);

            JobHandle dependency = determineTransitions;
            JobHandle projection = SetCurrentAnimation<float, CurrentProjection, ProjectionElement>(dependency);
            JobHandle thickness = SetCurrentAnimation<float, CurrentThickness, ThicknessElement>(dependency);
            JobHandle signalLength = SetCurrentAnimation<float, CurrentSignalLength, SignalLengthElement>(dependency);
            JobHandle sampleRate = SetCurrentAnimation<float, CurrentSampleRate, SampleRateElement>(dependency);
            
            JobHandle vertexDependency = JobHandle.CombineDependencies(signalLength, sampleRate);

            Entities.WithAll<Step>()
                    .ForEach((ref VertexCount count,
                              in CurrentSignalLength currentSignalLength,
                              in CurrentSampleRate currentSampleRate) =>
                    {
                        Animation<float> signal = currentSignalLength.Value;
                        Animation<float> sample = currentSampleRate.Value;
                        float maxSignalTime = math.max(signal.From, signal.To);
                        float maxSampleRate = math.max(sample.From, sample.To);
                        count.Value = 24 * (int)(maxSignalTime / (1f / maxSampleRate));
                    }).ScheduleParallel(vertexDependency);
            
            // update waves
            
            // If this throws errors (maybe material property block has race conditions), we can process in series
             
            UpdateAnimatableShaderFloatProperty<CurrentProjection>(projection, in projectionShaderProperty);
            UpdateAnimatableShaderFloatProperty<CurrentThickness>(thickness, in thicknessShaderProperty);
            UpdateAnimatableShaderFloatProperty<CurrentSignalLength>(signalLength, in signalLengthShaderProperty);
            UpdateAnimatableShaderFloatProperty<CurrentSampleRate>(sampleRate, in sampleRateShaderProperty);

            Entities.WithAll<Step>()
                    .ForEach((Entity entity, int index) => ecb.RemoveComponent<Step>(index, entity))
                    .ScheduleParallel(Dependency);
        }

        private JobHandle SetCurrentAnimation<TType, TCurrent, TBuffer>(JobHandle dependency) 
            where TType : new()
            where TCurrent : IValueSettable<Animation<TType>>
            where TBuffer : struct, IAnimatable, IValuable<TType>
        {
            return Entities.WithAll<Step>()
                           .WithoutBurst()
                           .ForEach((ref TCurrent current, ref DynamicBuffer<TBuffer> elements) =>
                           {
                               elements.RemoveAt(0);
                               TBuffer from = elements[0];
                               TType to = elements.Length == 1 ? elements[1].Value : from.Value;
                               current.Value = new Animation<TType>(from.Value, to, from.AnimationCurve);
                           })
                    .ScheduleParallel(dependency);
        }

        private JobHandle UpdateAnimatableShaderFloatProperty<TCurrent>(JobHandle dependency, 
                                                                        in AnimatableShaderProperty<float> property)
            where TCurrent : IValuable<Animation<float>>
        {
            AnimatableShaderProperty<float> shader = property;
            return Entities.WithAll<Step>()
                    .WithoutBurst()
                    .ForEach((in PropertyBlockReference propertyBlockReference,
                              in TCurrent current) =>
                    {
                        MaterialPropertyBlock block = (MaterialPropertyBlock)propertyBlockReference.Handle.Target;
                        Animation<float> value = current.Value;
                        block.SetFloat(shader.Animation.ID, (float)value.Curve);
                        block.SetFloat(shader.From.ID, value.From);
                        block.SetFloat(shader.To.ID, value.To);
                    }).ScheduleParallel(dependency);
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