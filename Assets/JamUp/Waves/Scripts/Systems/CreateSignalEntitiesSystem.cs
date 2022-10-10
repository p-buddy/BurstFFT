using System;
using System.Linq;
using System.Runtime.InteropServices;
using JamUp.Waves.Scripts.API;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
    public partial class CreateSignalEntitiesSystem: SystemBase
    {
        public const int MaxWaveCount = 10;
        private const float Attack = 0.01f;
        private const float Release = 0.01f;

        private Signal[] signals;
        
        private EntityQuery queryForArchetype;
        private EntityArchetype archetype;
        //private NativeArray<Entity> archetypeEntities;

        private EndInitializationEntityCommandBufferSystem endInitializationEcbSystem;
        private WaveDrawerSystem drawerSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            ComponentType[] typesForArchetype = AppDomain.CurrentDomain.GetAssemblies()
                                                         .SelectMany(asm => asm.GetTypes())
                                                         .Where(type =>
                                                                    typeof(IRequiredInArchetype).IsAssignableFrom(type))
                                                         .Select(type => (ComponentType)type)
                                                         .ToArray();
            
            archetype = EntityManager.CreateArchetype(typesForArchetype);
            queryForArchetype = GetEntityQuery(ComponentType.ReadOnly<IRequiredInArchetype>());

            endInitializationEcbSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
            drawerSystem = World.GetOrCreateSystem<WaveDrawerSystem>();
        }


        protected override void OnUpdate()
        {
            if (signals is null) return;
            
            NativeArray<Entity> archetypeEntities = queryForArchetype.ToEntityArray(Allocator.TempJob);
            EntityCommandBuffer.ParallelWriter ecb = endInitializationEcbSystem.CreateCommandBuffer().AsParallelWriter();

            for (int i = 0; i < signals.Length; i++)
            {
                CreateSignalEntity(signals[i], i, archetypeEntities, ecb);
            }
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

        private EntityForSignal GetEntity(NativeArray<Entity> existingEntities,
                                          EntityCommandBuffer.ParallelWriter ecb,
                                          int index,
                                          int frameCount,
                                          in KeyFrame frame)
        {
            bool useExisting = existingEntities.Length > index;
            NativeArray<Entity> entity = new NativeArray<Entity>(1, Allocator.TempJob);
            NativeArray<int> waveCount = new NativeArray<int>(1, Allocator.TempJob);

            JobHandle dependency;
            if (useExisting)
            {
                entity[0] = existingEntities[index];
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
                (int propertyBlockID, GCHandle gcHandle) = drawerSystem.GetPropertyBlockHandle();

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

        public void CreateSignalEntity(in Signal signal,
                                       int signalIndex,
                                       NativeArray<Entity> existingEntities,
                                       EntityCommandBuffer.ParallelWriter ecb)
        {
            KeyFrame[] frames = signal.Frames.ToArray();
            int frameCount = frames.Length;

            EntityForSignal entityForSignal = GetEntity(existingEntities, ecb, signalIndex, frameCount, frames[0]);
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
                    ? frames[index]
                    : index == frameCount
                        ? frames[frameCount - 1].DefaultAnimations(Release)
                        : frames[frameCount - 1].ZeroAmplitudes();
                
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

            var thickness = GetComponentDataFromEntity<CurrentThickness>();
            // Now just need to set currents based on whether or not the entity is freshly created

            // If not freshly created, than some lerping jobs must be done to see what the starting value should be
            // (whatever the current value is).

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
    }
}