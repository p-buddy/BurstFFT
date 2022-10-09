using System;
using System.Collections.Generic;
using System.Linq;
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
    public partial class WaveDrawerSystem : SystemBase
    {
        private const int MaxWaveCount = 10;
        private const float Attack = 0.01f;
        private const float Release = 0.01f;

        private int settingIndex;

        private Material material;

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
            ComponentType[] typesForArchetype = AppDomain.CurrentDomain.GetAssemblies()
                                                         .SelectMany(asm => asm.GetTypes())
                                                         .Where(type => typeof(IRequiredInArchetype).IsAssignableFrom(type))
                                                         .Select(type => (ComponentType)type)
                                                         .ToArray();
            
            archetype = EntityManager.CreateArchetype(typesForArchetype);
            
            endSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            Transform transform = new GameObject().transform;

            /*
            propertyBlock = new MaterialPropertyBlock();
            propertyBlock.Set(new ShaderProperty<Matrix4x4>("WaveOriginToWorldMatrix")
                                  .WithValue(transform.localToWorldMatrix));
            propertyBlock.Set(new ShaderProperty<Matrix4x4>("WorldToWaveOriginMatrix")
                                  .WithValue(transform.worldToLocalMatrix));
                                  */

            Object.Destroy(transform.gameObject);
            
            bounds = new Bounds(Vector3.zero, Vector3.one * 50f);
        }

        protected override void OnUpdate()
        {
            Draw();
            
            var ecb = endSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
            JobHandle updateDependency = EnqueueUpdates(Dependency, ecb);

            JobHandle waveUpdates = UpdateWaves(updateDependency);
            FloatAnimationProperties.UpdateAll(updateDependency, this);
            
            JobHandle vertexDependency = FloatAnimationProperties.GetUpdateHandle(
             FloatAnimationProperties.Kind.SampleRate, 
             FloatAnimationProperties.Kind.SignalLength);

            JobHandle vertexHandle = UpdateVertexCount(vertexDependency);

            // update waves
            
            JobHandle setHandle = FloatAnimationProperties.SetAll(this);
            Dependency = Cleanup(JobHandle.CombineDependencies(vertexHandle, setHandle), ecb);
        }

        private void Draw()
        {
            MeshTopology topology = MeshTopology.Triangles;
            ShadowCastingMode shadow = ShadowCastingMode.TwoSided;
            Entities.WithoutBurst()
                    .ForEach((in PropertyBlockReference propertyBlockRef, in VertexCount vertexCount) =>
                    {
                        MaterialPropertyBlock propertyBlock = propertyBlocks[propertyBlockRef.ID];
                        int count = vertexCount.Value;
                        Graphics.DrawProcedural(material, bounds, topology, count, 0, null, propertyBlock, shadow);
                    })
                    .Run();
        }

        private JobHandle EnqueueUpdates(JobHandle dependency, EntityCommandBuffer.ParallelWriter ecb)
        {
            float time = UnityEngine.Time.timeSinceLevelLoad;
            float delta = Time.DeltaTime;
            
            return Entities.WithNone<Step>()
                           .ForEach((Entity entity,
                                     int entityInQueryIndex,
                                     ref CurrentTimeFrame current,
                                     ref DynamicBuffer<DurationElement> elements) =>
                           {
                               if (!current.UpdateRequired(time, delta)) return;
                               if (elements.Length == 1)
                               {
                                   ecb.DestroyEntity(entityInQueryIndex, entity);
                                   return;
                               }
                               
                               elements.RemoveAt(0);
                               DurationElement duration = elements[0];
                               current.EndTime = time + duration;
                               ecb.AddComponent<Step>(entityInQueryIndex, entity);
                           }).ScheduleParallel(dependency);
        }

        private JobHandle UpdateWaves(JobHandle dependency)
        {
            return Entities.WithAll<Step>()
                           .ForEach((ref CurrentWaveCount currentWaveCount,
                                     ref DynamicBuffer<WaveCountElement> waveCounts,
                                     ref DynamicBuffer<AllWavesElement> allWaves,
                                     ref DynamicBuffer<CurrentWavesElement> currentWaves) =>
                           {
                               int startingWaveCount = waveCounts[0].Value;
                               int endingWaveCount = waveCounts[1].Value;
                               int waveCount = math.max(startingWaveCount, endingWaveCount);
                               currentWaves.ResizeUninitialized(waveCount);
                               currentWaveCount.Value = waveCount;
                               
                               for (int i = 0; i < waveCount; i++)
                               {
                                   int startIndex = i;
                                   int endIndex = startingWaveCount + i;

                                   bool startWaveInvalid = i > startingWaveCount;
                
                                   WaveState startingWave = startWaveInvalid
                                       ? allWaves[endIndex].Value.Value.ZeroedAmplitude
                                       : allWaves[startIndex].Value.Value;

                                   AnimationCurve animation = startWaveInvalid
                                       ? AnimationCurve.Linear
                                       : allWaves[startIndex].Value.AnimationCurve;

                                   WaveState endingWave = i > endingWaveCount
                                       ? allWaves[startIndex].Value.Value.ZeroedAmplitude
                                       : allWaves[endIndex].Value.Value;
                
                                   currentWaves[i] = new CurrentWavesElement
                                   {
                                       Value = WaveState.Pack(in startingWave, in endingWave, (float)animation)
                                   };
                               }
                               
                               waveCounts.RemoveAt(0);
                               allWaves.RemoveRange(0, startingWaveCount);
                           })
                           .ScheduleParallel(dependency);
        }

        private JobHandle UpdateVertexCount(JobHandle dependency)
        {
            return Entities.WithAll<Step>()
                    .ForEach((ref VertexCount count,
                              in CurrentSignalLength currentSignalLength,
                              in CurrentSampleRate currentSampleRate) =>
                    {
                        Animation<float> signal = currentSignalLength.Value;
                        Animation<float> sample = currentSampleRate.Value;
                        float maxSignalTime = math.max(signal.From, signal.To);
                        float maxSampleRate = math.max(sample.From, sample.To);
                        count.Value = 24 * (int)(maxSignalTime / (1f / maxSampleRate));
                    })
                    .ScheduleParallel(dependency);
        }

        private JobHandle Cleanup(JobHandle dependency, EntityCommandBuffer.ParallelWriter ecb)
        {
            return Entities.WithAll<Step>()
                    .ForEach((Entity entity, int entityInQueryIndex) =>
                                 ecb.RemoveComponent<Step>(entityInQueryIndex, entity))
                    .ScheduleParallel(dependency);
        }
       

        public KeyFrame GetCurrent(bool dispose = true)
        {
            float time = UnityEngine.Time.timeSinceLevelLoad;

            return new KeyFrame(Attack,
                                100,
                                ProjectionType.Perspective,
                                0.1f,
                                null,
                                1f
                               );
        }

        private readonly struct EntityInitializationHelper
        {
            public NativeArray<Entity> Entity { get; }
            public int SortKey { get; }
            public EntityCommandBuffer.ParallelWriter ECB { get; }
            public JobHandle Dependency { get; }

            public EntityInitializationHelper(NativeArray<Entity> entity,
                                       int sortKey,
                                       EntityCommandBuffer.ParallelWriter ecb,
                                       JobHandle dependency)
            {
                Entity = entity;
                SortKey = sortKey;
                ECB = ecb;
                Dependency = dependency;
            }

            public JobHandle SetCapacity<TBuffer>(int capacity) where TBuffer : struct, IBufferElementData =>
                new SetBufferCapacity<TBuffer>
                {
                    SortKey = SortKey,
                    Entity = Entity,
                    ECB = ECB,
                    Capacity = capacity
                }.Schedule(Dependency);


            public JobHandle AppendAnimatable<TBuffer>(Animatable<float> property) 
                where TBuffer : struct, IBufferElementData, IAnimatableSettable, IValueSettable<float>
            {
                return new AppendAnimationElement<float, Animatable<float>, TBuffer>
                {
                    ecb = ECB,
                    entity = Entity,
                    Property = property,
                    SortKey = SortKey
                }.Schedule(Dependency);
            }

            public JobHandle Append<TData, TElement>(TData data) where TData : new()
                                                                 where TElement : struct, IBufferElementData,
                                                                 IValueSettable<TData>
            {
                return new AppendElement<TData, TElement>
                {
                    ecb = ECB,
                    entity = Entity,
                    Data = data,
                    SortKey = SortKey
                }.Schedule(Dependency);
            }
        }
        
        public void CreateWaveEntity(in Signal signal)
        {
            int frameCount = signal.Frames.Count;
            var ecb = endSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
            
            NativeArray<Entity> entity = new NativeArray<Entity>(1, Allocator.TempJob);
            EntityArchetype localArchetype = archetype;
            JobHandle dependency = Job.WithBurst().WithCode(() =>
            {
                Entity ent = ecb.CreateEntity(0, localArchetype);
                entity[0] = ent;
            }).Schedule(Dependency);

            EntityInitializationHelper capacityHelper = new (entity, 1, ecb, dependency);

            int elementsToAdd = frameCount + 2;
            int capacity = elementsToAdd;
            int wavesEstimate = 2;
            capacityHelper.SetCapacity<DurationElement>(capacity);
            capacityHelper.SetCapacity<WaveCountElement>(capacity);
            capacityHelper.SetCapacity<ProjectionElement>(capacity);
            capacityHelper.SetCapacity<SignalLengthElement>(capacity);
            capacityHelper.SetCapacity<SampleRateElement>(capacity);
            capacityHelper.SetCapacity<ThicknessElement>(capacity);
            capacityHelper.SetCapacity<AllWavesElement>(capacity * wavesEstimate);
            capacityHelper.SetCapacity<CurrentWavesElement>(MaxWaveCount);

            int handlePerFrameOffset = 7;
            int sortKeyOffset = 2;
            int accumulatedWaveCount = 0;
            NativeArray<JobHandle> appendHandles = new (elementsToAdd * handlePerFrameOffset, Allocator.TempJob);
            for (int index = 0; index < elementsToAdd; index++)
            {
                bool isRelease = index >= frameCount;
                KeyFrame frame = !isRelease
                    ? signal.Frames[index]
                    : index == frameCount
                        ? signal.Frames[frameCount - 1].DefaultAnimations(Release)
                        : signal.Frames[frameCount - 1].ZeroAmplitudes();
                
                int sortKey = index + sortKeyOffset;
                EntityInitializationHelper helper = new (entity, index + sortKeyOffset, ecb, dependency);
                int baseIndex = index * handlePerFrameOffset;
                
                appendHandles[baseIndex] = helper.Append<float, DurationElement>(frame.Duration);
                appendHandles[baseIndex + 1] = helper.Append<int, WaveCountElement>(frame.Waves.Length);
                appendHandles[baseIndex + 2] = helper.AppendAnimatable<ProjectionElement>(frame.ProjectionFloat);
                appendHandles[baseIndex + 3] = helper.AppendAnimatable<SignalLengthElement>(frame.SignalLength);
                appendHandles[baseIndex + 4] = helper.AppendAnimatable<SampleRateElement>(frame.SignalLength);
                appendHandles[baseIndex + 5] = helper.AppendAnimatable<ThicknessElement>(frame.SignalLength);
                appendHandles[baseIndex + 6] = new AppendWaveElement
                {
                    ECB = ecb,
                    WaveStates = new NativeArray<Animatable<WaveState>>(frame.Waves, Allocator.TempJob),
                    SortKey = sortKey + accumulatedWaveCount,
                    Entity = entity
                }.Schedule(dependency);
                
                accumulatedWaveCount += frame.Waves.Length;
            }

            // Set currents
        }
    }
}