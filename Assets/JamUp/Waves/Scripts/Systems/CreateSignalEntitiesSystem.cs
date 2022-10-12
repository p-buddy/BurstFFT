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
    [BurstCompile]
    public partial class CreateSignalEntitiesSystem: SystemBase
    {
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
            
            ComponentDataFromEntity<CurrentTimeFrame> timeFrameForEntity = GetComponentDataFromEntity<CurrentTimeFrame>(true);
            ComponentDataFromEntity<CurrentWaveCount> waveCountForEntity = GetComponentDataFromEntity<CurrentWaveCount>(true);
            ComponentDataFromEntity<CurrentProjection> projectionForEntity = GetComponentDataFromEntity<CurrentProjection>(true);
            ComponentDataFromEntity<CurrentThickness> thicknessForEntity = GetComponentDataFromEntity<CurrentThickness>(true);
            ComponentDataFromEntity<CurrentSignalLength> signalLengthForEntity = GetComponentDataFromEntity<CurrentSignalLength>(true);
            ComponentDataFromEntity<CurrentSampleRate> sampleRateForEntity = GetComponentDataFromEntity<CurrentSampleRate>(true);

            BufferFromEntity<CurrentWavesElement> wavesForEntity = GetBufferFromEntity<CurrentWavesElement>(true);
            BufferFromEntity<CurrentWaveAxes> axesForEntity = GetBufferFromEntity<CurrentWaveAxes>(true);

            NativeArray<JobHandle> handles = new NativeArray<JobHandle>(signals.Count, Allocator.Temp);

            EntityArchetype localArchetype = archetype;
            float timeNow = UnityEngine.Time.timeSinceLevelLoad;
            
            for (int index = 0; index < signals.Count; index++)
            {
                EntityCommandBuffer ecb = beginEcbSystem.CreateCommandBuffer();
                
                Signal signal = signals[index];
                (NativeArray<CreateEntity.PackedFrame> frames, NativeArray<Animatable<WaveState>> waves) = CreateSignalEntity(in signal);

                JobHandle handle = new CreateEntity()
                {
                    PackedFrames = frames,
                    Waves = waves,
                    ExistingEntities = archetypeEntities,
                    Index = index,
                    ECB = ecb,
                    TimeNow = timeNow,
                    EntityArchetype = localArchetype,
                    TimeFrameForEntity = timeFrameForEntity,
                    WaveCountForEntity = waveCountForEntity,
                    ProjectionForEntity = projectionForEntity,
                    ThicknessForEntity = thicknessForEntity,
                    SignalLengthForEntity = signalLengthForEntity,
                    SampleRateForEntity = sampleRateForEntity,
                    WavesForEntity = wavesForEntity,
                    AxesForEntity = axesForEntity,

                }.Schedule(Dependency);

                frames.Dispose(handle);
                waves.Dispose(handle);

                handles[index] = handle;
            }

            Dependency = JobHandle.CombineDependencies(handles);
            beginEcbSystem.AddJobHandleForProducer(Dependency);
            
            handles.Dispose();
            archetypeEntities.Dispose(Dependency);

            signals.Clear();
            GC.Collect();
        }

        private (NativeArray<CreateEntity.PackedFrame>, NativeArray<Animatable<WaveState>>) CreateSignalEntity(in Signal signal)
        {
            int frameCount = signal.Frames.Count;
            NativeArray<CreateEntity.PackedFrame> packedFrames = new (signal.Frames.Count, Allocator.TempJob);
            int totalWaveCount = 0;
            
            for (int index = 0; index < frameCount; index++)
            {
                KeyFrame frame = signal.Frames[index];
                packedFrames[index] = CreateEntity.PackedFrame.Pack(in frame);
                totalWaveCount += frame.Waves.Length;
            }
            
            NativeArray<Animatable<WaveState>> waves = new (totalWaveCount, Allocator.TempJob);
            int accumulatedCount = 0;
            for (int index = 0; index < frameCount; index++)
            {
                Animatable<WaveState>[] states = signal.Frames[index].Waves;
                int count = states.Length;
                NativeArray<Animatable<WaveState>>.Copy(states, 0, waves, accumulatedCount, count);
                accumulatedCount += count;
            }

            return (packedFrames, waves);
        }
    }
}