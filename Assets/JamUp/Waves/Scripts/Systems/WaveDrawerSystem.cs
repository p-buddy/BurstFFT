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
using KeyFrame = JamUp.Waves.Scripts.API.KeyFrame;

namespace JamUp.Waves.Scripts
{
    public partial class WaveDrawerSystem : SystemBase
    {
        public const int MaxWaveCount = 10;
        private const float Attack = 0.01f;
        private const float Release = 0.01f;

        private int settingIndex;

        private Material material;

        private EntityArchetype archetype;
        private EndSimulationEntityCommandBufferSystem endSimulationEcbSystem;
        private EntityQuery queryForArchetype;
        
        private float2 currentTimeFrame;

        private Bounds bounds;

        private List<MaterialPropertyBlock> propertyBlocks;
        private List<GCHandle> handles;
        
        private AnimatableShaderProperty<float> projectionShaderProperty;
        private AnimatableShaderProperty<float> thicknessShaderProperty;
        private AnimatableShaderProperty<float> signalLengthShaderProperty;
        private AnimatableShaderProperty<float> sampleRateShaderProperty;

        private NativeArray<Entity>? archetypeEntities = null;

        protected override void OnCreate()
        {
            base.OnCreate();
            ComponentType[] typesForArchetype = AppDomain.CurrentDomain.GetAssemblies()
                                                         .SelectMany(asm => asm.GetTypes())
                                                         .Where(type => typeof(IRequiredInArchetype).IsAssignableFrom(type))
                                                         .Select(type => (ComponentType)type)
                                                         .ToArray();
            
            archetype = EntityManager.CreateArchetype(typesForArchetype);
            queryForArchetype = GetEntityQuery(ComponentType.ReadOnly<IRequiredInArchetype>());
            
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
            
            JobHandle setHandle = FloatAnimationProperties.SetAll(this);
            Dependency = Cleanup(JobHandle.CombineDependencies(vertexHandle, setHandle, waveUpdates), ecb);
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
            
            return Entities.WithNone<UpdateRequired>()
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
                               
                               DurationElement duration = elements[0];
                               current.StartTime = time;
                               current.EndTime = time + duration;
                               elements.RemoveAt(0);
                               ecb.AddComponent<UpdateRequired>(entityInQueryIndex, entity);
                           }).ScheduleParallel(dependency);
        }

        private JobHandle UpdateWaves(JobHandle dependency)
        {
            return Entities.WithAll<UpdateRequired>()
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
            return Entities.WithAll<UpdateRequired>()
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
            return Entities.WithAll<UpdateRequired>()
                    .ForEach((Entity entity, int entityInQueryIndex) =>
                                 ecb.RemoveComponent<UpdateRequired>(entityInQueryIndex, entity))
                    .ScheduleParallel(dependency);
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
            
            public JobHandle Reset<TBuffer>(int capacity) where TBuffer : struct, IBufferElementData =>
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

        // Should be called once before signals are added
        public void RefreshEntityQuery()
        {
            archetypeEntities?.Dispose();
            archetypeEntities = queryForArchetype.ToEntityArray(Allocator.TempJob);
        }

        private readonly struct EntityForSignal
        {
            public int NextSortKey { get; }
            
            public NativeArray<Entity> Entity { get; }
            
            public JobHandle Dependency { get; }
            
            public bool FreshlyCreated { get; }

            public EntityForSignal(NativeArray<Entity> entity, int currentSortKey, JobHandle dependency, bool freshlyCreated)
            {
                Entity = entity;
                NextSortKey = currentSortKey + 1;
                Dependency = dependency;
                FreshlyCreated = freshlyCreated;
            }
        }

        private EntityForSignal GetEntity(EntityCommandBuffer.ParallelWriter ecb,
                                                                int index,
                                                                int frameCount,
                                                                in KeyFrame frame)
        {
            bool useExisting = archetypeEntities.HasValue && archetypeEntities.Value.Length > index;
            NativeArray<Entity> entity = new NativeArray<Entity>(1, Allocator.TempJob);
            NativeArray<int> waveCount = new NativeArray<int>(1, Allocator.TempJob);

            JobHandle dependency;
            if (useExisting)
            {
                entity[0] = archetypeEntities.Value[index];
                waveCount[0] = frame.Waves.Length;

                dependency =  Job.WithBurst()
                                 .WithReadOnly(entity)
                                 .WithCode(() =>
                                 {
                                     Entity ent = entity[0];
                                     ecb.RemoveComponent<UpdateRequired>(0, ent);
                                 })
                                 .Schedule(Dependency);
            }
            else
            {
                EntityArchetype localArchetype = archetype;
                int maxCurrentWaveCount = MaxWaveCount;
                
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                GCHandle gcHandle = GCHandle.Alloc(block);
                int propertyBlockID = propertyBlocks.Count;
                propertyBlocks.Add(block);
                handles.Add(gcHandle);
                
                dependency = Job.WithBurst().WithCode(() =>
                {
                    Entity ent = ecb.CreateEntity(0, localArchetype);
                    ecb.SetComponent(1, ent, new PropertyBlockReference(propertyBlockID, gcHandle));
                    ecb.SetBuffer<CurrentWavesElement>(1, ent).EnsureCapacity(maxCurrentWaveCount);
                    entity[0] = ent;
                }).Schedule(Dependency);

                ComponentDataFromEntity<CurrentWaveCount> getWaveCount = GetComponentDataFromEntity<CurrentWaveCount>();
                dependency = Job.WithBurst()
                                .WithReadOnly(entity)
                                .WithReadOnly(getWaveCount)
                                .WithCode(() =>
                                {
                                    waveCount[0] = getWaveCount[entity[0]].Value;
                                }).Schedule(dependency);
            }

            int elementsToAdd = 1 + frameCount + 2;
            int capacity = elementsToAdd;
            int wavesEstimate = 2;
            JobHandle ensureBuffers = Job.WithBurst()
                                         .WithReadOnly(entity)
                                         .WithCode(() =>
                                         {
                                             int sortKey = 1;
                                             Entity ent = entity[0];
                                             ecb.SetBuffer<DurationElement>(sortKey, ent).EnsureCapacity(capacity);
                                             ecb.SetBuffer<ProjectionElement>(sortKey, ent).EnsureCapacity(capacity);
                                             ecb.SetBuffer<SignalLengthElement>(sortKey, ent).EnsureCapacity(capacity);
                                             ecb.SetBuffer<SampleRateElement>(sortKey, ent).EnsureCapacity(capacity);
                                             ecb.SetBuffer<ThicknessElement>(sortKey, ent).EnsureCapacity(capacity);

                                             sortKey += 1;
                                             ecb.AppendToBuffer<DurationElement>(sortKey, ent, default);
                                             ecb.AppendToBuffer<ProjectionElement>(sortKey, ent, default);
                                             ecb.AppendToBuffer<SignalLengthElement>(sortKey, ent, default);
                                             ecb.AppendToBuffer<SampleRateElement>(sortKey, ent, default);
                                             ecb.SetBuffer<ThicknessElement>(sortKey, ent).EnsureCapacity(capacity);
                                         })
                                         .Schedule(dependency);

            JobHandle waveBuffers = Job.WithBurst()
                                              .WithReadOnly(entity)
                                              .WithReadOnly(waveCount)
                                              .WithDisposeOnCompletion(waveCount)
                                              .WithCode(() =>
                                              {
                                                  int sortKey = 1;
                                                  Entity ent = entity[0];
                                                  int count = waveCount[0];
                                                  int estimate = (capacity - 1) * wavesEstimate + count;
                                                  
                                                  ecb.SetBuffer<WaveCountElement>(sortKey, ent).EnsureCapacity(capacity);
                                                  ecb.SetBuffer<AllWavesElement>(sortKey, ent).EnsureCapacity(estimate);
                                                  
                                                  sortKey += 1;
                                                  ecb.AppendToBuffer(sortKey, ent, new WaveCountElement
                                                  {
                                                      Value = waveCount[0],
                                                  });

                                                  for (int i = 0; i < count; i++)
                                                  {
                                                      ecb.AppendToBuffer<AllWavesElement>(sortKey, ent, default);
                                                  }
                                              })
                                              .Schedule(dependency);

            return new(entity, 2, JobHandle.CombineDependencies(ensureBuffers, waveBuffers), !useExisting);
        }

        public void CreateWaveEntity(in Signal signal, int signalIndex)
        {
            // Consider queue up signals and then adding them all at the beginning of the update loop
            
            int frameCount = signal.Frames.Count;
            EntityCommandBuffer.ParallelWriter ecb = endSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();

            EntityForSignal entityForSignal = GetEntity(ecb, signalIndex, frameCount, signal.Frames[0]);
            JobHandle dependency = entityForSignal.Dependency;
            NativeArray<Entity> entity = entityForSignal.Entity;

            int elementsToAdd = frameCount + 2;
            int handlePerFrameOffset = 7;
            int sortKeyOffset = entityForSignal.NextSortKey;
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
                int baseHandleIndex = index * handlePerFrameOffset;
                
                appendHandles[baseHandleIndex] = helper.Append<float, DurationElement>(frame.Duration);
                appendHandles[baseHandleIndex + 1] = helper.Append<int, WaveCountElement>(frame.Waves.Length);
                appendHandles[baseHandleIndex + 2] = helper.AppendAnimatable<ProjectionElement>(frame.ProjectionFloat);
                appendHandles[baseHandleIndex + 3] = helper.AppendAnimatable<SignalLengthElement>(frame.SignalLength);
                appendHandles[baseHandleIndex + 4] = helper.AppendAnimatable<SampleRateElement>(frame.SignalLength);
                appendHandles[baseHandleIndex + 5] = helper.AppendAnimatable<ThicknessElement>(frame.SignalLength);
                appendHandles[baseHandleIndex + 6] = new AppendWaveElement
                {
                    ECB = ecb,
                    WaveStates = new NativeArray<Animatable<WaveState>>(frame.Waves, Allocator.Temp),
                    SortKey = sortKey + accumulatedWaveCount,
                    Entity = entity
                }.Schedule(dependency);
                
                accumulatedWaveCount += frame.Waves.Length;
            }
            
            // Now just need to set currents based on whether or not the entity is freshly created
            
            // If not freshly created, than some lerping jobs must be done to see what the starting value should be
            // (whatever the current value is).
            
        }
    }
}