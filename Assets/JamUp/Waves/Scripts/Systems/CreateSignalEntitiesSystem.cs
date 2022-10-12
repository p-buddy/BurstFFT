using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using JamUp.Waves.Scripts.API;
using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using KeyFrame = JamUp.Waves.Scripts.API.KeyFrame;


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
                new WaveState(1f, 1f, 0f, WaveType.Sine, new Vector(0f, 1f, 0f))
            }, 1f));
            EnqueueSignal(in signal);
        }


        protected override void OnUpdate()
        {
            if (signals.Count == 0) return;
            
            NativeArray<Entity> archetypeEntities = queryForArchetype.ToEntityArray(Allocator.TempJob);

            NativeArray<JobHandle> handles = new NativeArray<JobHandle>(signals.Count, Allocator.Temp);
            for (int i = 0; i < signals.Count; i++)
            {
                EntityCommandBuffer ecb = beginEcbSystem.CreateCommandBuffer();
                handles[i] = CreateSignalEntity(signals[i], i, archetypeEntities, ecb);
            }

            Dependency = JobHandle.CombineDependencies(handles);
            handles.Dispose();
            archetypeEntities.Dispose(Dependency);


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
        
        private struct PackedFrame
        {
            public float Duration;
            public int WaveCount;
            public Animatable<float> ProjectionType;
            public Animatable<float> SampleRate;
            public Animatable<float> SignalLength;
            public Animatable<float> Thickness;

            public PackedFrame DefaultAnimations(float duration)
            {
                Duration = duration;
                ProjectionType = new Animatable<float>(ProjectionType.Value);
                SampleRate = new Animatable<float>(SampleRate.Value);
                SignalLength = new Animatable<float>(SignalLength.Value);
                Thickness = new Animatable<float>(Thickness.Value);
                return this;
            }
        };

        private static PackedFrame PackFrame(in KeyFrame frame) => new()
        {
            Duration = frame.Duration,
            WaveCount = frame.Waves.Length,
            ProjectionType = new Animatable<float>((int)frame.ProjectionType.Value, frame.ProjectionType.AnimationCurve),
            SampleRate = new Animatable<float>(frame.SampleRate.Value, frame.SampleRate.AnimationCurve),
            SignalLength = frame.SignalLength,
            Thickness = frame.Thickness
        };

        [BurstCompile]
        private struct CreateEntity: IJob
        {
            public struct CommandBufferForEntity
            {
                public EntityCommandBuffer CommandBuffer;
                public Entity Entity;
                public void Append<TBufferElement>(TBufferElement element) where TBufferElement : struct, IBufferElementData
                    => CommandBuffer.AppendToBuffer(Entity, element);

                public void AppendAnimationElement<TBufferElement>(Animatable<float> property)
                    where TBufferElement : struct, IBufferElementData, IAnimatableSettable, IValueSettable<float> 
                    => Append(new TBufferElement { Value = property.Value, AnimationCurve = property.AnimationCurve });

                public void AppendWaveElements(NativeSlice<Animatable<WaveState>> waves)
                {
                    for (int i = 0; i < waves.Length; i++)
                    {
                        Animatable<WaveState> waveState = waves[i];
                        Append(waveState.Value.AsWaveElement(waveState.AnimationCurve));
                    }
                }
            }
            
            public struct BufferHelper<TBufferElement> where TBufferElement : struct, IBufferElementData
            {
                public CommandBufferForEntity CBfE;
                public void Append(TBufferElement element) => CBfE.Append(element);
            }
            
            private const int MaxWaveCount = 10;
            private const float AttackTime = 0.1f;
            private const float ReleaseTime = 0.01f;


            [ReadOnly] 
            public NativeArray<Entity> ExistingEntities;

            [ReadOnly] 
            public NativeArray<PackedFrame> PackedFrames;
            
            [ReadOnly]
            public NativeArray<Animatable<WaveState>> Waves;

            public float TimeNow;
            
            public bool UseExisting;

            public int Index;
            
            public int FrameCount;

            public EntityCommandBuffer ECB;

            public EntityArchetype EntityArchetype;

            [ReadOnly]
            public ComponentDataFromEntity<CurrentTimeFrame> TimeFrameForEntity;
            [ReadOnly]
            public ComponentDataFromEntity<CurrentWaveCount> WaveCountForEntity;
            [ReadOnly]
            public ComponentDataFromEntity<CurrentProjection> ProjectionForEntity;
            [ReadOnly]
            public ComponentDataFromEntity<CurrentSignalLength> SignalLengthForEntity;
            [ReadOnly]
            public ComponentDataFromEntity<CurrentSampleRate> SampleRateForEntity;
            [ReadOnly]
            public ComponentDataFromEntity<CurrentThickness> ThicknessForEntity;
            [ReadOnly]
            public BufferFromEntity<CurrentWavesElement> WavesForEntity;
            [ReadOnly]
            public BufferFromEntity<CurrentWaveAxes> AxesForEntity;

            private Entity entity;
            private float interpolant;

            public void Execute()
            {
                entity = UseExisting ? ExistingEntities[Index] : ECB.CreateEntity(EntityArchetype);
                interpolant = UseExisting ? TimeFrameForEntity[entity].Interpolant(TimeNow) : 0f;

                if (UseExisting)
                {
                    ECB.RemoveComponent<UpdateRequired>(entity);
                    NativeArray<CurrentWavesElement> currentWaves = WavesForEntity[entity].AsNativeArray();
                    NativeArray<CurrentWaveAxes> currentWaveAxes = AxesForEntity[entity].AsNativeArray();

                    for (int i = 0; i < currentWaves.Length; i++)
                    {
                        AllWavesElement lerp = AllWavesElement.FromLerp(interpolant, currentWaves[i], currentWaveAxes[i]);
                        ECB.AppendToBuffer(entity, lerp);
                    }
                }
                else
                {
                    Init<CurrentWavesElement>(MaxWaveCount);
                }

                int elementsToAdd = FrameCount + 2; // all frames, plus 'sustain' and 'release'
                int capacity = 1 + elementsToAdd; // prepend one frame for 'attack'
                
                Init<DurationElement>(capacity).Append(new DurationElement(AttackTime));
                Init<WaveCountElement>(capacity).Append(new WaveCountElement
                {
                    Value = UseExisting ? WaveCountForEntity[entity].Value : 0
                });

                Init<ProjectionElement>(capacity).Append(First<ProjectionElement, CurrentProjection>(ProjectionForEntity));
                Init<SignalLengthElement>(capacity).Append(First<SignalLengthElement, CurrentSignalLength>(SignalLengthForEntity));
                Init<SampleRateElement>(capacity).Append(First<SampleRateElement, CurrentSampleRate>(SampleRateForEntity));
                Init<ThicknessElement>(capacity).Append(First<ThicknessElement, CurrentThickness>(ThicknessForEntity));

                CommandBufferForEntity entityCommandBuffer = new ()
                {
                    CommandBuffer = ECB,
                    Entity = entity
                };

                int accumulatedWaveCounts = 0;
                for (int index = 0; index < elementsToAdd; index++)
                {
                    bool isRelease = index >= FrameCount;
                    bool isFinal = index > FrameCount;
                    PackedFrame frame = !isRelease
                        ? PackedFrames[index]
                        : isFinal
                            ? PackedFrames[FrameCount - 1] // nothing, modifications will be to waves
                            : PackedFrames[FrameCount - 1].DefaultAnimations(ReleaseTime);

                    int waveCount = frame.WaveCount;
                    entityCommandBuffer.Append(new WaveCountElement { Value = waveCount });
                    entityCommandBuffer.Append(new DurationElement(frame.Duration));
                    
                    entityCommandBuffer.AppendAnimationElement<ProjectionElement>(frame.ProjectionType);
                    entityCommandBuffer.AppendAnimationElement<SignalLengthElement>(frame.SignalLength);
                    entityCommandBuffer.AppendAnimationElement<SampleRateElement>(frame.SampleRate);
                    entityCommandBuffer.AppendAnimationElement<ThicknessElement>(frame.Thickness);

                    NativeSlice<Animatable<WaveState>> waveSlice = new(Waves, accumulatedWaveCounts, waveCount);
                    entityCommandBuffer.AppendWaveElements(waveSlice);
                    
                    accumulatedWaveCounts += waveCount;
                }
            }

            private BufferHelper<TBufferElement> Init<TBufferElement>(int capacity)
                where TBufferElement : struct, IBufferElementData
            {
                ECB.SetBuffer<TBufferElement>(entity).EnsureCapacity(capacity);
                return new BufferHelper<TBufferElement> {CBfE = new ()
                {
                    Entity = entity,
                    CommandBuffer = ECB,
                }};
            }

            private TBufferElement First<TBufferElement, TComponent>(ComponentDataFromEntity<TComponent> dataFromEntity)
                where TBufferElement : struct, IBufferElementData, IValueSettable<float>, IAnimatableSettable
                where TComponent : struct, IComponentData, IValuable<Animation<float>>
            {
                float value = UseExisting ? dataFromEntity[entity].Value.Lerp(interpolant) : default;
                return new TBufferElement
                {
                    Value = value,
                    AnimationCurve = AnimationCurve.Linear
                };
            }

            private NativeArray<float4x2> GetCurrentWaves()
            {
                // float4x2 = 4 rows 2 columns;
                
                // Idea, use the last index of matrix to encode other wave type

                // TODO
                return default;
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
                /*dependency = new AppendWaveElement
                {
                    ECB = ecb,
                    WaveStates = new NativeArray<Animatable<WaveState>>(frame.Waves, Allocator.TempJob),
                    Entity = entity
                }.Schedule(dependency);*/
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