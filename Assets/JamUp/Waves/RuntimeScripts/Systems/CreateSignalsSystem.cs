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
        private EntityQuery queryForArchetype;
        private EntityArchetype archetype;

        private BeginSimulationEntityCommandBufferSystem beginEcbSystem;
        private DrawerSystem drawerSystem;
        private SynthesizerSystem synthSystem;

        private ManagedResource<ThreadSafeAPI> api;

        private readonly Queue<ExecutionProcess> processQueue = new ();
        
        private ExecutionProcess? currentProcess;
        private JobHandle? processHandle;

        public void ExecuteString(string code)
        {
            ExecuteJavascriptJob.Builder builder = new(code, in api);
            EnqueueProcess(new ExecutionProcess(in builder));
        }
        
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

            api = ManagedResource<ThreadSafeAPI>.Hold(new ThreadSafeAPI());
        }

        protected override void OnUpdate()
        {
            if (!currentProcess.HasValue)
            {
                if (processQueue.TryDequeue(out ExecutionProcess nextProcess))
                {
                    currentProcess = nextProcess;
                }
                return;
            }

            ExecutionProcess current = currentProcess.Value;

            if (!processHandle.HasValue)
            {
                processHandle = current.Start(Dependency);
                return;
            }

            JobHandle handle = processHandle.Value;
            
            if (!handle.IsCompleted) return;
            handle.Complete();
            
            CreateEntities(in current);
            Reset();
        }

        private void CreateEntities(in ExecutionProcess executionProcess)
        {
            ComponentsForJob componentsForJob = new ComponentsForJob(queryForArchetype);
            var archetypeEntities = queryForArchetype.ToEntityArrayAsync(Allocator.Persistent, out JobHandle entityHandle);
            JobHandle dependency = JobHandle.CombineDependencies(entityHandle, componentsForJob.Dependency, Dependency);
            
            EntityCommandBuffer ecb = beginEcbSystem.CreateCommandBuffer();
            BufferFromEntity<CurrentWavesElement> wavesForEntity = GetBufferFromEntity<CurrentWavesElement>(true);
            BufferFromEntity<CurrentWaveAxes> axesForEntity = GetBufferFromEntity<CurrentWaveAxes>(true);
            
            Dependency = executionProcess.ScheduleCreationJob(archetype,
                                                              archetypeEntities,
                                                              in componentsForJob,
                                                              ecb,
                                                              wavesForEntity,
                                                              axesForEntity,
                                                              drawerSystem,
                                                              synthSystem,
                                                              JobHandle.CombineDependencies(Dependency, dependency));
            
            beginEcbSystem.AddJobHandleForProducer(Dependency);
        }

        private void Reset()
        {
            currentProcess = null;
            processHandle = null;
        }
        
        private void EnqueueProcess(in ExecutionProcess process)
        {
            if (currentProcess.HasValue)
            {
                processQueue.Enqueue(process);
                return;
            }

            currentProcess = process;
        }

        private readonly struct ExecutionProcess
        {
            public ExecuteJavascriptJob.Builder ExecutionBuilder { get; }
            public float TimeNow { get; }
            
            public ExecutionProcess(in ExecuteJavascriptJob.Builder builder)
            {
                ExecutionBuilder = builder;
                TimeNow = UnityEngine.Time.timeSinceLevelLoad;
            }

            public JobHandle Start(JobHandle dependency) => ExecutionBuilder.MakeJob().Schedule(dependency);

            public JobHandle ScheduleCreationJob(EntityArchetype archetype,
                                                 NativeArray<Entity> archetypeEntities,
                                                 in ComponentsForJob componentsForJob, 
                                                 EntityCommandBuffer ecb, 
                                                 BufferFromEntity<CurrentWavesElement> wavesPerEntity,
                                                 BufferFromEntity<CurrentWaveAxes> axesPerEntity,
                                                 DrawerSystem drawerSystem, // systems can implement generic interface maybe
                                                 SynthesizerSystem synthSystem,
                                                 JobHandle dependency)
            {
                int signalsCount = ExecutionBuilder.RootFrequencies.Length;
                int newEntityCount = math.max(signalsCount - componentsForJob.EntityCount, 0);
                NativeArray<PropertyBlockReference> propertyBlockReferences = new(newEntityCount, Allocator.TempJob);
                NativeArray<AudioGraphReference> audioGraphReferences = new(newEntityCount, Allocator.TempJob);

                for (int i = 0; i < newEntityCount; i++)
                {
                    (int id, GCHandle drawerHandle) = drawerSystem.GetPropertyBlockHandle();
                    propertyBlockReferences[i] = new PropertyBlockReference(id, drawerHandle);

                    int index = synthSystem.GetGraphReference();
                    audioGraphReferences[i] = new AudioGraphReference(index);
                }
                
#if MULTITHREADED
                JobHandle handle = new CreateEntities
#else
                JobHandle handle = dependency.CompleteAndGetBack();
                new CreateEntities
#endif
                {
                    PackedFrames = ExecutionBuilder.FrameData.AsDeferredJobArray(),
                    FrameOffsets = ExecutionBuilder.FrameDataOffsets.AsDeferredJobArray(),
                    Waves = ExecutionBuilder.WaveStates.AsDeferredJobArray(),
                    WaveOffsets = ExecutionBuilder.WaveStateOffsets.AsDeferredJobArray(),
                    PreviousJobExecutionTime = ExecutionBuilder.ExecutionTime,
                    RootFrequencies = ExecutionBuilder.RootFrequencies.AsDeferredJobArray(),
                    ExistingEntities = archetypeEntities,
                    ECB = ecb,
                    DataRetrievalTime = TimeNow,
                    PropertyBlocks = propertyBlockReferences,
                    GraphReferences = audioGraphReferences,
                    EntityArchetype = archetype,
                    TimeFrames = componentsForJob.TimeFrames,
                    WaveCounts = componentsForJob.WaveCounts,
                    ProjectionForEntity = componentsForJob.Projections,
                    ThicknessForEntity = componentsForJob.Thicknesses,
                    SignalLengthForEntity = componentsForJob.SignalLengths,
                    SampleRateForEntity = componentsForJob.SampleRates,
                    WavesForEntity = wavesPerEntity,
                    AxesForEntity = axesPerEntity,
                }
#if MULTITHREADED
                    .Schedule(dependency);
#else
                    .Run();
#endif
                
                ExecutionBuilder.Dispose(handle);
                archetypeEntities.Dispose(handle);
                componentsForJob.Dispose(handle);
                propertyBlockReferences.Dispose(handle);
                audioGraphReferences.Dispose(handle);
                
                return handle;
            }
        }
        
        private readonly struct ComponentsForJob
        {
            public int EntityCount { get; }
            public NativeArray<CurrentTimeFrame> TimeFrames { get; }
            public NativeArray<CurrentWaveCount> WaveCounts { get; }
            public NativeArray<CurrentProjection> Projections { get; }
            public NativeArray<CurrentThickness> Thicknesses { get; }
            public NativeArray<CurrentSignalLength> SignalLengths { get; }
            public NativeArray<CurrentSampleRate> SampleRates { get; }
            public JobHandle Dependency { get; }
            public ComponentsForJob(EntityQuery query)
            {
                EntityCount = query.CalculateEntityCount();
                
                if (EntityCount == 0)
                {
                    TimeFrames = new (0, Allocator.Persistent);
                    WaveCounts = new (0, Allocator.Persistent);
                    Projections = new (0, Allocator.Persistent);
                    Thicknesses =  new (0, Allocator.Persistent);
                    SignalLengths = new (0, Allocator.Persistent);
                    SampleRates = new (0, Allocator.Persistent);
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
                query.ToComponentDataArrayAsync<TComponent>(Allocator.Persistent, out handle);

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
    }
}