using System.Collections.Generic;
using System.Runtime.InteropServices;
using JamUp.UnityUtility;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace JamUp.Waves.RuntimeScripts
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SignalManagementSystem))]
    public partial class DrawerSystem: SystemBase
    {
        private Material material;
        
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
            //LogPropertyBlocks();
            
            JobHandle setTimeHandle = SetTimeOnPropertyBlock(Dependency);
            JobHandle setWavesHandle = SetWavesOnPropertyBlock(setTimeHandle);

            JobHandle vertexDependency = FloatAnimationProperties.GetUpdateHandle(
             FloatAnimationProperties.Kind.SampleRate, 
             FloatAnimationProperties.Kind.SignalLength);
            
            JobHandle vertexHandle = UpdateVertexCount(vertexDependency);
            JobHandle setWavesAndPropertiesHandle = FloatAnimationProperties.SetAll(this, setWavesHandle);
            Dependency = JobHandle.CombineDependencies(vertexHandle, setWavesAndPropertiesHandle);
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

        private void LogPropertyBlocks()
        {
            for (var index = 0; index < propertyBlocks.Count; index++)
            {
                MaterialPropertyBlock block = propertyBlocks[index];
                Debug.Log($"Property Block {index}:\n{block.ReadoutAll()}");
            }
        }

        private JobHandle SetWavesOnPropertyBlock(JobHandle dependency)
        {
            int waveCountId = waveCountProperty.ID;
            int wavesId = waveDataProperty.ID;
            int waveAxesId = waveAxesProperty.ID;
            
#if MULTITHREADED
            JobHandle handle = Entities
#else
            JobHandle handle = dependency.CompleteAndGetBack();
            Entities
#endif
                .WithAll<UpdateRequired>()
                .WithName("SetWavesOnPropertyBlock")
                .WithoutBurst()
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
                })
#if MULTITHREADED
                .ScheduleParallel(dependency);
#else
                .Run();
#endif
            return handle; 
        }
        
        private JobHandle SetTimeOnPropertyBlock(JobHandle dependency)
        {
            int startId = startTimeProperty.ID;
            int endId = endTimeProperty.ID;
#if MULTITHREADED
            JobHandle handle = Entities
#else
            JobHandle handle = dependency.CompleteAndGetBack();
            Entities
#endif
                .WithAll<UpdateRequired>()
                .WithName("SetTimeOnPropertyBlock")
                .WithoutBurst()
                .WithAll<UpdateRequired>()
                .ForEach((in PropertyBlockReference propertyBlock,
                          in CurrentTimeFrame timeFrame) =>
                {
                    MaterialPropertyBlock block = (MaterialPropertyBlock)propertyBlock.Handle.Target;
                    block.SetFloat(startId, timeFrame.StartTime);
                    block.SetFloat(endId, timeFrame.EndTime);
                })
#if MULTITHREADED
                .ScheduleParallel(dependency);
#else
                .Run();
#endif
            return handle; 
        }

        private JobHandle UpdateVertexCount(JobHandle dependency)
        {
#if MULTITHREADED
            JobHandle handle = Entities
#else
            JobHandle handle = dependency.CompleteAndGetBack();
            Entities
#endif
                .WithAll<UpdateRequired>()
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
            return handle;
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