using System.Collections.Generic;
using System.Runtime.InteropServices;
using JamUp.UnityUtility;
using JamUp.Waves.Scripts.API;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using KeyFrame = JamUp.Waves.Scripts.API.KeyFrame;

namespace JamUp.Waves.Scripts
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class WaveDrawerSystem : SystemBase
    {
        private int settingIndex;

        private Material material;

        private EndSimulationEntityCommandBufferSystem endSimulationEcbSystem;
        
        private Bounds bounds;

        private Queue<int> freePropertyBlockIDs;
        private List<MaterialPropertyBlock> propertyBlocks;
        private List<GCHandle> handles;

        private Matrix4x4 localToWorld;
        private Matrix4x4 worldToLocal;
        
        protected override void OnCreate()
        {
            base.OnCreate();

            endSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            Transform transform = new GameObject().transform;
            localToWorld = transform.localToWorldMatrix;
            worldToLocal = transform.worldToLocalMatrix;
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
            
            MaterialPropertyBlock newBlock = new MaterialPropertyBlock();
            newBlock.Set(new ShaderProperty<Matrix4x4>("WaveOriginToWorldMatrix").WithValue(localToWorld));
            newBlock.Set(new ShaderProperty<Matrix4x4>("WorldToWaveOriginMatrix").WithValue(worldToLocal));
            
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