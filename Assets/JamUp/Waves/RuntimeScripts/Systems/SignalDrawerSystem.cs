using System.Collections.Generic;
using System.Runtime.InteropServices;
using JamUp.UnityUtility;
using JamUp.Waves.RuntimeScripts.BufferIndexing;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace JamUp.Waves.RuntimeScripts
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class SignalDrawerSystem : SystemBase
    {
        private int settingIndex;

        private Material material;

        private EndSimulationEntityCommandBufferSystem endSimulationEcbSystem;
        
        private Bounds bounds;

        private readonly Queue<int> freePropertyBlockIDs = new ();
        private readonly List<MaterialPropertyBlock> propertyBlocks = new();
        private readonly List<GCHandle> handles = new ();
        
        private readonly ShaderProperty<int> waveCountProperty = new ("WaveCount");
        private readonly ShaderProperty<Matrix4x4[]> waveDataProperty = new ("WaveTransitionData");
        private readonly ShaderProperty<float[]> waveAxesProperty = new ("WaveAxesData");

        private readonly ShaderProperty<float> startTimeProperty = new("StartTime");
        private readonly ShaderProperty<float> endTimeProperty = new("EndTime");

        private ShaderProperty<Matrix4x4> waveOriginToWorldProperty;
        private ShaderProperty<Matrix4x4> worldToWaveOriginProperty;

        protected override void OnCreate()
        {
            base.OnCreate();

            endSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            Transform transform = new GameObject().transform;
            Matrix4x4 localToWorld = transform.localToWorldMatrix;
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            Object.Destroy(transform.gameObject);

            waveOriginToWorldProperty = new ShaderProperty<Matrix4x4>("WaveOriginToWorldMatrix").WithValue(localToWorld);
            worldToWaveOriginProperty = new ShaderProperty<Matrix4x4>("WorldToWaveOriginMatrix").WithValue(worldToLocal);
            
            bounds = new Bounds(Vector3.zero, Vector3.one * 50f);
            material = Resources.Load<Material>("WaveDrawer");
            Assert.IsNotNull(material);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            foreach (GCHandle handle in handles)
            {
                handle.Free();
            }
        }

        protected override void OnUpdate()
        {
            Draw();
            
            var ecb = endSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
            JobHandle updatesHandle = EnqueueUpdates(Dependency, ecb);

            JobHandle waveUpdates = UpdateWaves(updatesHandle);
            FloatAnimationProperties.UpdateAll(updatesHandle, this);
            
            JobHandle setTimeHandle = SetTime(updatesHandle);
            JobHandle setWavesHandle = SetWaves(JobHandle.CombineDependencies(setTimeHandle, waveUpdates));

            JobHandle vertexDependency = FloatAnimationProperties.GetUpdateHandle(
             FloatAnimationProperties.Kind.SampleRate, 
             FloatAnimationProperties.Kind.SignalLength);
            
            JobHandle vertexHandle = UpdateVertexCount(vertexDependency);
            JobHandle setWavesAndPropertiesHandle = FloatAnimationProperties.SetAll(this, setWavesHandle);
            
            Dependency = Cleanup(JobHandle.CombineDependencies(vertexHandle, setWavesAndPropertiesHandle), ecb);
            endSimulationEcbSystem.AddJobHandleForProducer(Dependency);

            foreach (MaterialPropertyBlock block in propertyBlocks)
            {
                Debug.Log(block.ReadoutAll());
            }
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
            
#if MULTITHREADED
            JobHandle handle = Entities.WithNone<UpdateRequired>()
#else
            JobHandle handle = dependency.CompleteAndGetBack();
            Entities.WithNone<UpdateRequired>()
#endif
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
            JobHandle handle = Entities.WithAll<UpdateRequired>()
#else
            JobHandle handle = dependency.CompleteAndGetBack();
            Entities.WithAll<UpdateRequired>()
#endif
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
                        int startingWaveCount = waveCounts[index.Value].Value;
                        int endingWaveCount = waveCounts[index.Value + 1].Value;
                        int waveCount = math.max(startingWaveCount, endingWaveCount);

                        currentWaves.ResizeUninitialized(waveCount);
                        currentAxes.ResizeUninitialized(waveCount);

                        for (int i = 0; i < waveCount; i++)
                        {
                            int startIndex = waveIndex.Value + i;
                            int endIndex = waveIndex.Value + startingWaveCount + i;
                                   
                            AllWavesElement startingWave = i >= startingWaveCount
                                ? allWaves[endIndex].Default
                                : allWaves[startIndex];

                            AllWavesElement endingWave = i >= endingWaveCount
                                ? allWaves[startIndex].Default
                                : allWaves[endIndex];
                
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
                        waveIndex.IncrementBy(startingWaveCount);
                    })
#if MULTITHREADED
                    .ScheduleParallel(dependency);
#else
                    .Run();
#endif
            return handle;
        }

        private JobHandle SetWaves(JobHandle dependency)
        {
            int waveCountId = waveCountProperty.ID;
            int wavesId = waveDataProperty.ID;
            int waveAxesId = waveAxesProperty.ID;
            
            return Entities.WithoutBurst()
                           .WithAll<UpdateRequired>()
                           .WithName("SetWaves")
                           .ForEach((in PropertyBlockReference propertyBlock,
                                     in CurrentWaveCount waveCount,
                                     in DynamicBuffer<CurrentWavesElement> waves,
                                     in DynamicBuffer<CurrentWaveAxes> axes) =>
                           {
                               MaterialPropertyBlock block = (MaterialPropertyBlock)propertyBlock.Handle.Target;
                               block.SetInt(waveCountId, waveCount.Value);
                                                
                               NativeArray<float4x4> nativeWaves = waves.ToNativeArray(Allocator.Temp)
                                                                        .Reinterpret<float4x4>();
                               
                               for (int i = 0; i < nativeWaves.Length; i++)
                               {
                                   nativeWaves[i] = math.transpose(nativeWaves[i]);
                               }
                               block.SetMatrixArray(wavesId, nativeWaves.Reinterpret<Matrix4x4>().ToArray());
                               nativeWaves.Dispose();
                                                
                               NativeArray<float> nativeAxes = axes.AsNativeArray()
                                                                   .Reinterpret<float3x2>()
                                                                   .Reinterpret<float>(sizeof(float) * 6);
                               block.SetFloatArray(waveAxesId, nativeAxes.ToArray());
                           }).ScheduleParallel(dependency);
        }
        
        private JobHandle SetTime(JobHandle dependency)
        {
            int startId = startTimeProperty.ID;
            int endId = endTimeProperty.ID;
            return Entities.WithoutBurst()
                           .WithName("SetTime")
                           .WithAll<UpdateRequired>()
                           .ForEach((in PropertyBlockReference propertyBlock,
                                     in CurrentTimeFrame timeFrame) =>
                           {
                               MaterialPropertyBlock block = (MaterialPropertyBlock)propertyBlock.Handle.Target;
                               block.SetFloat(startId, timeFrame.StartTime);
                               block.SetFloat(endId, timeFrame.EndTime);
                           }).ScheduleParallel(dependency);
        }

        private JobHandle UpdateVertexCount(JobHandle dependency)
        {
#if MULTITHREADED
            JobHandle handle = Entities.WithAll<UpdateRequired>()
#else
            JobHandle handle = dependency.CompleteAndGetBack();
            Entities.WithAll<UpdateRequired>()
#endif
                    .WithName("UpdateVertexCount")
                    .WithAll<SignalEntity>()
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
#if MULTITHREADED
                    .ScheduleParallel(dependency);
#else
                    .Run();
#endif
            return handle;        }

        private JobHandle Cleanup(JobHandle dependency, EntityCommandBuffer.ParallelWriter ecb)
        {
            return Entities.WithAll<UpdateRequired>()
                           .WithName("Cleanup")
                           .WithAll<SignalEntity>()
                           .ForEach((Entity entity, int entityInQueryIndex) =>
                                                                ecb.RemoveComponent<UpdateRequired>(entityInQueryIndex, entity))
                           .ScheduleParallel(dependency);
        }

        public (int id, GCHandle handle) GetPropertyBlockHandle()
        {
            if (freePropertyBlockIDs.Count > 0)
            {
                int id = freePropertyBlockIDs.Dequeue();
                MaterialPropertyBlock freeBlock = propertyBlocks[id];
                GCHandle handle = GCHandle.Alloc(freeBlock);
                handles[id] = handle;
                return (id, handle);
            }
            
            MaterialPropertyBlock newBlock = new();
            newBlock.Set(waveOriginToWorldProperty);
            newBlock.Set(worldToWaveOriginProperty);
            
            newBlock.Set(waveDataProperty.WithValue(new Matrix4x4[CreateEntity.MaxWaveCount]));
            newBlock.Set(waveAxesProperty.WithValue(new float[CreateEntity.MaxWaveCount * 3 * 2]));
            
            GCHandle newHandle = GCHandle.Alloc(newBlock);
            propertyBlocks.Add(newBlock);
            handles.Add(newHandle);
            return (propertyBlocks.Count - 1, newHandle);
        }
        
        private void ReleasePropertyBlock(int id)
        {
            handles[id].Free();
            freePropertyBlockIDs.Enqueue(id);
        }
    }
}