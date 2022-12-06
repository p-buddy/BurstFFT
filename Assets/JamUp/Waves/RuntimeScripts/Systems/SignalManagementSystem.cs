using JamUp.Waves.RuntimeScripts.BufferIndexing;
using Unity.Entities;
using Unity.Jobs;

namespace JamUp.Waves.RuntimeScripts
{
    public partial class SignalManagementSystem: SystemBase
    {
        private EndSimulationEntityCommandBufferSystem endSimulationEcbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            endSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = endSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
            JobHandle updatesHandle = EnqueueUpdates(Dependency, ecb);

            Dependency = UpdateWaves(updatesHandle);
            FloatAnimationProperties.UpdateAll(updatesHandle, this);

            Dependency = Cleanup(Dependency, ecb);
            endSimulationEcbSystem.AddJobHandleForProducer(Dependency);
        }

        private JobHandle EnqueueUpdates(JobHandle dependency, EntityCommandBuffer.ParallelWriter ecb)
        {
            float time = UnityEngine.Time.timeSinceLevelLoad;
            float delta = Time.DeltaTime;
            
#if MULTITHREADED
            JobHandle handle = Entities
#else
            JobHandle handle = dependency.CompleteAndGetBack();
            Entities
#endif
                    .WithNone<UpdateRequired>()
                    .WithName("EnqueueUpdates")
                    .WithAll<SignalEntity>()
                    .ForEach((Entity entity,
                              int entityInQueryIndex,
                              ref CurrentIndex index,
                              ref CurrentTimeFrame current,
                              in DynamicBuffer<DurationElement> elements, 
                              in LastIndex last) =>
                    {
                        if (index.Value >= 0 && !current.UpdateRequired(time, delta)) return;
                               
                        if (index.IsLast(in last))
                        {
                            ecb.DestroyEntity(entityInQueryIndex, entity);
                            return;
                        }
                        
                        index.Increment();
                        DurationElement duration = elements[index.Value];
                        current.StartTime = time;
                        current.EndTime = time + duration.Value;
                        ecb.AddComponent<UpdateRequired>(entityInQueryIndex, entity);
                    })
#if MULTITHREADED
                    .ScheduleParallel(dependency);
#else
                    .Run();
#endif
            return handle;
        }

        private JobHandle UpdateWaves(JobHandle dependency)
        {
#if MULTITHREADED
            JobHandle handle = Entities
#else
            JobHandle handle = dependency.CompleteAndGetBack();
            Entities
#endif
                .WithAll<UpdateRequired>()
                .WithName("UpdateWaves")
                .WithAll<SignalEntity>()
                .ForEach((ref CurrentWaveCount currentWaveCount,
                          ref DynamicBuffer<CurrentWavesElement> currentWaves, 
                          ref DynamicBuffer<CurrentWaveAxes> currentAxes,
                          ref CurrentWaveIndex waveIndex,
                          in DynamicBuffer<WaveCountElement> waveCounts,
                          in DynamicBuffer<AllWavesElement> allWaves,
                          in CurrentIndex index) =>
                {
                    AllWavesElement.Indexer indexer = new (index.Value, in waveCounts, in allWaves);

                    int waveCount = indexer.ComputedWaveCount;

                    currentWaves.ResizeUninitialized(waveCount);
                    currentAxes.ResizeUninitialized(waveCount);

                    for (int i = 0; i < waveCount; i++)
                    {
                        indexer.GetWavesAt(waveIndex.Value, i, out var startingWave, out var endingWave);

                        currentWaves[i] = new CurrentWavesElement
                        {
                            Value = AllWavesElement.PackSettings(startingWave, endingWave)
                        };
                            
                        currentAxes[i] = new CurrentWaveAxes
                        {
                            Value = AllWavesElement.PackAxes(startingWave, endingWave)
                        };
                    }

                    currentWaveCount.Value = waveCount;
                    waveIndex.IncrementBy(indexer.CountToIncrement);
                })
#if MULTITHREADED
                    .ScheduleParallel(dependency);
#else
                .Run();
#endif
            return handle;
        }

        private JobHandle Cleanup(JobHandle dependency, EntityCommandBuffer.ParallelWriter ecb)
        {
            return Entities.WithAll<UpdateRequired>()
                           .WithName("Cleanup")
                           .WithAll<SignalEntity>()
                           .ForEach((Entity entity, int entityInQueryIndex) =>
                                                                ecb.RemoveComponent<UpdateRequired>(entityInQueryIndex, entity))
                           .ScheduleParallel(dependency);
        }
    }
}