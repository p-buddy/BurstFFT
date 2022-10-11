using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using JamUp.Waves.Scripts.API;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace JamUp.Waves.Scripts
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(BeginSimulationEntityCommandBufferSystem))]
    [AlwaysUpdateSystem]
    public partial class CreateSignalEntitiesSystem: SystemBase
    {
        public const int MaxWaveCount = 10;
        private const float Attack = 0.01f;
        private const float Release = 0.01f;

        private readonly List<Signal> signals = new();
        
        private EntityQuery queryForArchetype;
        private EntityArchetype archetype;

        private BeginSimulationEntityCommandBufferSystem beginEcbSystem;
        private SignalDrawerSystem drawerSystem;

        public void EnqueueSignal(in Signal signal) => signals.Add(signal);

        protected override void OnCreate()
        {
            base.OnCreate();
            ComponentType[] typesForArchetype = AppDomain.CurrentDomain.GetAssemblies()
                                                         .Where(asm => !asm.FullName.StartsWith("Microsoft"))
                                                         .SelectMany(asm => asm.GetTypes())
                                                         .Where(type => typeof(IRequiredInArchetype).IsAssignableFrom(type))
                                                         .Where(type => type.IsValueType)
                                                         .Select(type => new ComponentType(type))
                                                         .ToArray();
            
            archetype = EntityManager.CreateArchetype(typesForArchetype);
            queryForArchetype = GetEntityQuery(ComponentType.ReadOnly<SignalEntity>());

            beginEcbSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            drawerSystem = World.GetOrCreateSystem<SignalDrawerSystem>();
            
            Signal signal = new Signal(100f);
            signal.AddFrame(new KeyFrame(10f, 100, ProjectionType.Perspective, 1.0f, new []
            {
                new WaveState(1f, 1f, 0f, WaveType.Sine, new SimpleFloat3(0f, 1f, 0f))
            }, 1f));
            EnqueueSignal(in signal);
        }


        protected override void OnUpdate()
        {
            if (signals.Count == 0) return;
            
            NativeArray<Entity> archetypeEntities = queryForArchetype.ToEntityArray(Allocator.TempJob);

            try
            {
                NativeArray<JobHandle> handles = new NativeArray<JobHandle>(signals.Count, Allocator.Temp);
                for (int i = 0; i < signals.Count; i++)
                {
                    EntityCommandBuffer ecb = beginEcbSystem.CreateCommandBuffer();
                    handles[i] = CreateSignalEntity(signals[i], i, archetypeEntities, ecb);
                }

                Dependency = JobHandle.CombineDependencies(handles);
                handles.Dispose();
                archetypeEntities.Dispose(Dependency);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            signals.Clear();
        }

        private readonly struct EntityForSignal
        {
            public NativeArray<Entity> Entity { get; }
            
            public JobHandle Dependency { get; }
            
            public bool FreshlyCreated { get; }

            public EntityForSignal(NativeArray<Entity> entity, JobHandle dependency, bool freshlyCreated)
            {
                Entity = entity;
                Dependency = dependency;
                FreshlyCreated = freshlyCreated;
            }
        }

        private EntityForSignal GetEntity(NativeArray<Entity> existingEntities,
                                          EntityCommandBuffer ecb,
                                          int index,
                                          int frameCount)
        {
            bool useExisting = existingEntities.Length > index;
            NativeArray<Entity> entity = new NativeArray<Entity>(1, Allocator.TempJob);
            NativeArray<int> waveCount = new NativeArray<int>(1, Allocator.TempJob);

            JobHandle dependency;
            if (useExisting)
            {
                entity[0] = existingEntities[index];

                dependency =  Job.WithBurst()
                                 .WithReadOnly(entity)
                                 .WithCode(() =>
                                 {
                                     Entity ent = entity[0];
                                     ecb.RemoveComponent<UpdateRequired>(ent);
                                 })
                                 .Schedule(Dependency);
                
                ComponentDataFromEntity<CurrentWaveCount> getWaveCount = GetComponentDataFromEntity<CurrentWaveCount>();
                dependency = Job.WithBurst()
                                .WithReadOnly(entity)
                                .WithReadOnly(getWaveCount)
                                .WithCode(() =>
                                {
                                    waveCount[0] = getWaveCount[entity[0]].Value;
                                }).Schedule(dependency);
            }
            else
            {
                EntityArchetype localArchetype = archetype;
                int maxCurrentWaveCount = MaxWaveCount;
                (int propertyBlockID, GCHandle gcHandle) = drawerSystem.GetPropertyBlockHandle();
                waveCount[0] = 0;

                dependency = Job.WithBurst().WithCode(() =>
                {
                    Entity ent = ecb.CreateEntity(localArchetype);
                    ecb.SetComponent(ent, new PropertyBlockReference(propertyBlockID, gcHandle));
                    ecb.SetBuffer<CurrentWavesElement>(ent).EnsureCapacity(maxCurrentWaveCount);
                    entity[0] = ent;
                }).Schedule(Dependency);
            }

            int elementsToAdd = 1 + frameCount + 2;
            int capacity = elementsToAdd;
            int wavesEstimate = 2;
            float localAttack = Attack;
            JobHandle ensureBuffers = Job.WithBurst()
                                         .WithReadOnly(entity)
                                         .WithCode(() =>
                                         {
                                             Entity ent = entity[0];
                                             ecb.SetBuffer<DurationElement>(ent).EnsureCapacity(capacity);
                                             ecb.SetBuffer<ProjectionElement>(ent).EnsureCapacity(capacity);
                                             ecb.SetBuffer<SignalLengthElement>(ent).EnsureCapacity(capacity);
                                             ecb.SetBuffer<SampleRateElement>(ent).EnsureCapacity(capacity);
                                             ecb.SetBuffer<ThicknessElement>(ent).EnsureCapacity(capacity);

                                             ecb.AppendToBuffer(ent, new DurationElement(localAttack));
                                             ecb.AppendToBuffer<ProjectionElement>(ent, default);
                                             ecb.AppendToBuffer<SignalLengthElement>(ent, default);
                                             ecb.AppendToBuffer<SampleRateElement>(ent, default);
                                             ecb.SetBuffer<ThicknessElement>(ent).EnsureCapacity(capacity);
                                         })
                                         .Schedule(dependency);

            JobHandle waveBuffers = Job.WithBurst()
                                              .WithReadOnly(entity)
                                              .WithReadOnly(waveCount)
                                              .WithDisposeOnCompletion(waveCount)
                                              .WithCode(() =>
                                              {
                                                  Entity ent = entity[0];
                                                  int count = waveCount[0];
                                                  int estimate = (capacity - 1) * wavesEstimate + count;
                                                  
                                                  ecb.SetBuffer<WaveCountElement>(ent).EnsureCapacity(capacity);
                                                  ecb.SetBuffer<AllWavesElement>(ent).EnsureCapacity(estimate);
                                                  
                                                  ecb.AppendToBuffer(ent, new WaveCountElement
                                                  {
                                                      Value = waveCount[0],
                                                  });

                                                  for (int i = 0; i < count; i++)
                                                  {
                                                      ecb.AppendToBuffer<AllWavesElement>(ent, default);
                                                  }
                                              })
                                              .Schedule(ensureBuffers);
            
            // Now just need to set currents based on whether or not the entity is freshly created

            // If not freshly created, than some lerping jobs must be done to see what the starting value should be
            // (whatever the current value is).

            return new(entity, waveBuffers, !useExisting);
        }

        private JobHandle CreateSignalEntity(in Signal signal, 
                                             int signalIndex, 
                                             NativeArray<Entity> existingEntities, 
                                             EntityCommandBuffer ecb)
        {
            KeyFrame[] frames = signal.Frames.ToArray();
            int frameCount = frames.Length;

            EntityForSignal entityForSignal = GetEntity(existingEntities, ecb, signalIndex, frameCount);
            NativeArray<Entity> entity = entityForSignal.Entity;

            int elementsToAdd = frameCount + 2;
            
            JobHandle dependency = entityForSignal.Dependency;
            
            for (int index = 0; index < elementsToAdd; index++)
            {
                bool isRelease = index >= frameCount;
                KeyFrame frame = !isRelease
                    ? frames[index]
                    : index == frameCount
                        ? frames[frameCount - 1].DefaultAnimations(Release)
                        : frames[frameCount - 1].ZeroAmplitudes();
                
                EntityInitializationHelper helper = new (entity, ecb, dependency);
                
                dependency = helper.Append<float, DurationElement>(frame.Duration, dependency);
                dependency = helper.Append<int, WaveCountElement>(frame.Waves.Length, dependency);
                dependency = helper.AppendAnimatable<ProjectionElement>(frame.ProjectionFloat, dependency);
                dependency = helper.AppendAnimatable<SignalLengthElement>(frame.SignalLength, dependency);
                dependency = helper.AppendAnimatable<SampleRateElement>(frame.SignalLength, dependency);
                dependency = helper.AppendAnimatable<ThicknessElement>(frame.SignalLength, dependency);
                dependency = new AppendWaveElement
                {
                    ECB = ecb,
                    WaveStates = new NativeArray<Animatable<WaveState>>(frame.Waves, Allocator.TempJob),
                    Entity = entity
                }.Schedule(dependency);
            }

            return dependency;
        }
        
        private readonly struct EntityInitializationHelper
        {
            public NativeArray<Entity> Entity { get; }
            public EntityCommandBuffer ECB { get; }
            public JobHandle Dependency { get; }

            public EntityInitializationHelper(NativeArray<Entity> entity,
                                       EntityCommandBuffer ecb,
                                       JobHandle dependency)
            {
                Entity = entity;
                ECB = ecb;
                Dependency = dependency;
            }

            public JobHandle AppendAnimatable<TBuffer>(Animatable<float> property, JobHandle dependency) 
                where TBuffer : struct, IBufferElementData, IAnimatableSettable, IValueSettable<float>
            {
                return new AppendAnimationElement<float, Animatable<float>, TBuffer>
                {
                    ecb = ECB,
                    entity = Entity,
                    Property = property,
                }.Schedule(dependency);
            }

            public JobHandle Append<TData, TElement>(TData data, JobHandle dependency) where TData : new()
                                                                 where TElement : struct, IBufferElementData,
                                                                 IValueSettable<TData>
            {
                return new AppendElement<TData, TElement>
                {
                    ecb = ECB,
                    entity = Entity,
                    Data = data,
                }.Schedule(dependency);
            }
        }
    }
}