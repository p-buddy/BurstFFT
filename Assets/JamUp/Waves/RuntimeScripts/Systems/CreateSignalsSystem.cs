using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using JamUp.Waves.RuntimeScripts.API;
using JamUp.Waves.RuntimeScripts.Audio;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace JamUp.Waves.RuntimeScripts
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(BeginSimulationEntityCommandBufferSystem))]
    [AlwaysUpdateSystem]
    [BurstCompile]
    public partial class CreateSignalsSystem: SystemBase
    {
        private readonly List<Signal> signals = new();
        
        private EntityQuery queryForArchetype;
        private EntityArchetype archetype;

        private BeginSimulationEntityCommandBufferSystem beginEcbSystem;
        private DrawerSystem drawerSystem;
        private SynthesizerSystem synthSystem;

        private GCHandle apiHandle;

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
            queryForArchetype = GetEntityQuery(archetype.GetComponentTypes());

            beginEcbSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            drawerSystem = World.GetOrCreateSystem<DrawerSystem>();
            synthSystem = World.GetOrCreateSystem<SynthesizerSystem>();

            apiHandle = new ThreadSafeAPI().GetHandle();
        }

        private readonly struct ComponentsForJob
        {
            public NativeArray<CurrentTimeFrame> TimeFrames { get; }
            public NativeArray<CurrentWaveCount> WaveCounts { get; }
            public NativeArray<CurrentProjection> Projections { get; }
            public NativeArray<CurrentThickness> Thicknesses { get; }
            public NativeArray<CurrentSignalLength> SignalLengths { get; }
            public NativeArray<CurrentSampleRate> SampleRates { get; }
            public JobHandle Dependency { get; }
            public ComponentsForJob(EntityQuery query, int entityCount)
            {
                if (entityCount == 0)
                {
                    TimeFrames = new (0, Allocator.TempJob);
                    WaveCounts = new (0, Allocator.TempJob);
                    Projections = new (0, Allocator.TempJob);
                    Thicknesses =  new (0, Allocator.TempJob);
                    SignalLengths = new (0, Allocator.TempJob);
                    SampleRates = new (0, Allocator.TempJob);
                    Dependency = default;
                    return;
                }
                
                TimeFrames = GetComponents<CurrentTimeFrame>(query, out JobHandle a);
                WaveCounts = GetComponents<CurrentWaveCount>(query, out JobHandle b);
                Projections = GetComponents<CurrentProjection>(query, out JobHandle c);
                Thicknesses = GetComponents<CurrentThickness>(query, out JobHandle d);
                SignalLengths = GetComponents<CurrentSignalLength>(query, out JobHandle e);
                SampleRates = GetComponents<CurrentSampleRate>(query, out JobHandle f);
                Dependency = JobHandle.CombineDependencies(JobHandle.CombineDependencies(a, b, c),
                                                           JobHandle.CombineDependencies(d, e, f));
            }
            
            private static NativeArray<TComponent> GetComponents<TComponent>(EntityQuery query, out JobHandle handle)
                where TComponent : struct, IComponentData =>
                query.ToComponentDataArrayAsync<TComponent>(Allocator.TempJob, out handle);

            public void Dispose(JobHandle dependency)
            {
                TimeFrames.Dispose(dependency);
                WaveCounts.Dispose(dependency);
                Projections.Dispose(dependency);
                Thicknesses.Dispose(dependency);
                SampleRates.Dispose(dependency);
                SignalLengths.Dispose(dependency);
            }
        }

        protected override void OnUpdate()
        {
            int signalCount = signals.Count;
            
            if (signalCount == 0) return;

            int entityCount = queryForArchetype.CalculateEntityCount();
            
            int newEntityCount = math.max(signalCount - entityCount, 0);
            NativeArray<PropertyBlockReference> propertyBlockReferences = new(newEntityCount, Allocator.TempJob);
            NativeArray<AudioGraphReference> audioGraphReferences = new(newEntityCount, Allocator.TempJob);

            for (int i = 0; i < newEntityCount; i++)
            {
                (int id, GCHandle handle) = drawerSystem.GetPropertyBlockHandle();
                propertyBlockReferences[i] = new PropertyBlockReference(id, handle);

                int index = synthSystem.GetGraphReference();
                audioGraphReferences[i] = new AudioGraphReference(index);
            }

            var archetypeEntities = queryForArchetype.ToEntityArrayAsync(Allocator.TempJob, out JobHandle entityHandle);
            ComponentsForJob componentsForJob = new ComponentsForJob(queryForArchetype, entityCount);

            JobHandle dependency = JobHandle.CombineDependencies(entityHandle, componentsForJob.Dependency, Dependency);
            
            BufferFromEntity<CurrentWavesElement> wavesForEntity = GetBufferFromEntity<CurrentWavesElement>(true);
            BufferFromEntity<CurrentWaveAxes> axesForEntity = GetBufferFromEntity<CurrentWaveAxes>(true);

            NativeArray<JobHandle> handles = new NativeArray<JobHandle>(signals.Count, Allocator.Temp);

            EntityArchetype localArchetype = archetype;
            float timeNow = UnityEngine.Time.timeSinceLevelLoad;
            
            for (int index = 0; index < signals.Count; index++)
            {
                EntityCommandBuffer ecb = beginEcbSystem.CreateCommandBuffer();
                
                Signal signal = signals[index];
                var signalData = ExtractDataFromSignal(in signal);
                
#if MULTITHREADED
                JobHandle handle = new CreateEntity
#else
                JobHandle handle = dependency.CompleteAndGetBack();
                new CreateEntity
#endif
                {
                    PackedFrames = signalData.FrameData,
                    Waves = signalData.WaveData,
                    ExistingEntities = archetypeEntities,
                    Index = index,
                    ECB = ecb,
                    TimeNow = timeNow,
                    RootFrequency = signal.RootFrequency,
                    PropertyBlocks = propertyBlockReferences,
                    GraphReferences = audioGraphReferences,
                    EntityArchetype = localArchetype,
                    TimeFrames = componentsForJob.TimeFrames,
                    WaveCounts = componentsForJob.WaveCounts,
                    ProjectionForEntity = componentsForJob.Projections,
                    ThicknessForEntity = componentsForJob.Thicknesses,
                    SignalLengthForEntity = componentsForJob.SignalLengths,
                    SampleRateForEntity = componentsForJob.SampleRates,
                    WavesForEntity = wavesForEntity,
                    AxesForEntity = axesForEntity,
                }
#if MULTITHREADED
                    .Schedule(dependency);
#else
                    .Run();
#endif
                signalData.Dispose();

                handles[index] = handle;
            }

            Dependency = JobHandle.CombineDependencies(handles);
            handles.Dispose();

            beginEcbSystem.AddJobHandleForProducer(Dependency);
            archetypeEntities.Dispose(Dependency);
            propertyBlockReferences.Dispose(Dependency);
            audioGraphReferences.Dispose(Dependency);
            componentsForJob.Dispose(Dependency);

            signals.Clear();
        }

        private readonly struct BlittableSignalData: IDisposable
        {
            public NativeArray<CreateEntity.PackedFrame> FrameData { get; }
            public NativeArray<Animatable<WaveState>> WaveData { get; }
            
            public BlittableSignalData(NativeArray<CreateEntity.PackedFrame> frameData,
                                       NativeArray<Animatable<WaveState>> waveData)
            {
                FrameData = frameData;
                WaveData = waveData;
            }
            
            public void Dispose()
            {
                FrameData.Dispose();
                WaveData.Dispose();
            }
        }
        
        private BlittableSignalData ExtractDataFromSignal(in Signal signal)
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

            return new (packedFrames, waves);
        }

        public void ExecuteString(string code)
        {
            ExecuteJavascriptJob.Builder builder = new(code, apiHandle);
            ExecuteJavascriptJob job = builder.MakeJob();
            
            //var handle = job.Schedule();
            //builder.Dispose(handle);
            //handle.Complete();
            
            job.Run();
            builder.Dispose();
        }
        
    }
}