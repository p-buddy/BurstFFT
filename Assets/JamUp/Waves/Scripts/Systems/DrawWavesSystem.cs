using JamUp.UnityUtility;
using JamUp.Waves.Scripts.API;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace JamUp.Waves.Scripts.Systems
{
    public partial class DrawWavesSystem: SystemBase
    {
        private const int MaxWaveCount = 10;
        
        private Material material;

        private struct ShaderPropertiesBlittable
        {
            public ShaderProperty<int> WaveCount;
            public ShaderProperty<float> StartTime;
            public ShaderProperty<float> EndTime;

            public AnimatableShaderProperty<int> SampleRate;
            public AnimatableShaderProperty<float> Thickness;
            public AnimatableShaderProperty<float> SignalTime;

            public void Apply(MaterialPropertyBlock block)
            {
                block.Set(WaveCount);
                block.Set(StartTime);
                block.Set(EndTime);
                block.Set(SampleRate);
                block.Set(Thickness);
                block.Set(SignalTime);
            }
        }

        private NativeArray<ShaderPropertiesBlittable> currentBlittableSettings;
        
        // Wave shader representation: float4x4
        // row 1: ...start wave (4 floats / ints)
        // row 2: ...start propagation axis (3 floats) + animation curve (1 int)
        // row 3: ...end wave (4 floats / ints)
        // row 4: ...end propagation axis (3 floats) & one FREE slot
        private NativeArray<float4x4> nativeCurrentWaves;
        private ShaderProperty<Matrix4x4[]> currentWavesSetting;

        private NativeArray<int> numberOfVertices;

        private EntityArchetype archetype;
        private EndSimulationEntityCommandBufferSystem endSimulationEcbSystem;
        private float2 currentTimeFrame;

        private Bounds bounds;
        private MaterialPropertyBlock propertyBlock;


        protected override void OnCreate()
        {
            base.OnCreate();
            archetype = EntityManager.CreateArchetype(typeof(TransitionState));
            endSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            Transform transform = new GameObject().transform;
            
            propertyBlock = new MaterialPropertyBlock();
            propertyBlock.Set(new ShaderProperty<UnityEngine.Matrix4x4>("WaveOriginToWorldMatrix")
                                  .WithValue(transform.localToWorldMatrix));
            propertyBlock.Set(new ShaderProperty<UnityEngine.Matrix4x4>("WorldToWaveOriginMatrix")
                                  .WithValue(transform.worldToLocalMatrix));
            
            Object.Destroy(transform.gameObject);

            currentBlittableSettings = new NativeArray<ShaderPropertiesBlittable>(1, Allocator.Persistent);
            currentBlittableSettings[0] = new ()
            {   
                WaveCount = new (nameof(ShaderPropertiesBlittable.WaveCount)),
                StartTime = new (nameof(ShaderPropertiesBlittable.StartTime)),
                EndTime = new (nameof(ShaderPropertiesBlittable.EndTime)),
                SampleRate = new (nameof(ShaderPropertiesBlittable.SampleRate)),
                SignalTime = new (nameof(ShaderPropertiesBlittable.SignalTime)),
                Thickness = new (nameof(ShaderPropertiesBlittable.Thickness)),
            };

            nativeCurrentWaves = new NativeArray<float4x4>(MaxWaveCount, Allocator.Persistent);
            currentWavesSetting = new ShaderProperty<Matrix4x4[]>("WaveTransitionData").WithValue(new Matrix4x4[MaxWaveCount]);
            numberOfVertices = new NativeArray<int>(1, Allocator.Persistent);
            bounds = new Bounds(Vector3.zero, Vector3.one * 50f);
        }

        protected override void OnUpdate()
        {
            if (currentTimeFrame.y < (float)Time.ElapsedTime)
            {
                // perform job to update current settings and waves
                
                nativeCurrentWaves.Reinterpret<Matrix4x4>().CopyTo(currentWavesSetting.Value);
                propertyBlock.Set(currentWavesSetting);
                
                currentBlittableSettings[0].Apply(propertyBlock);
                
                // Put into job
                ShaderPropertiesBlittable current = currentBlittableSettings[0];
                float maxSignalTime = Mathf.Max(current.SignalTime.From.Value, current.SignalTime.To.Value);
                int maxSampleRate = System.Math.Max(current.SampleRate.From.Value, current.SampleRate.To.Value);
                numberOfVertices[0] = 24 * (int)(maxSignalTime / (1f / maxSampleRate));
            }

            Graphics.DrawProcedural(material, bounds, MeshTopology.Triangles, numberOfVertices[0], 0, null, propertyBlock, ShadowCastingMode.TwoSided);
        }
        
        public void CreateWaveAnimation(in Signal signal, float time)
        {
            int frameCount = signal.Frames.Count;

            KeyFrame.JobFriendlyRepresentation capture = new KeyFrame.JobFriendlyRepresentation
            {
                Projections = new NativeArray<AnimatableProperty<ProjectionType>>(frameCount, Allocator.TempJob),
                SampleRates = new NativeArray<AnimatableProperty<int>>(frameCount, Allocator.TempJob),
                SignalLengths = new NativeArray<AnimatableProperty<float>>(frameCount, Allocator.TempJob),
                Thicknesses = new NativeArray<AnimatableProperty<float>>(frameCount, Allocator.TempJob),
                Durations = new NativeArray<float>(frameCount, Allocator.TempJob),
                WaveCounts = new NativeArray<int>(frameCount, Allocator.TempJob)
            };
            
            for (int index = 0; index < frameCount; index++)
            {
                signal.Frames[index].CaptureForJob(capture, index);
            }

            NativeArray<int> runningTotalWaveCounts = new (frameCount, Allocator.TempJob);
            new CalculateRunningTotalJob.Int
            {
                Input = capture.WaveCounts,
                RunningTotal = runningTotalWaveCounts
            }.Run();
            
            NativeArray<float> runningTotalDurations = new (frameCount, Allocator.TempJob);
            new CalculateRunningTotalJob.Float
            {
                Input = capture.Durations,
                RunningTotal = runningTotalDurations
            }.Run();

            int totalWaves = runningTotalWaveCounts[frameCount - 1];
            NativeArray<AnimatableProperty<WaveState>> waves = new (totalWaves, Allocator.TempJob);
            
            for (int index = 1; index < signal.Frames.Count; index++)
            {
                signal.Frames[index].CaptureWaves(waves, runningTotalWaveCounts[index - 1]);
            }
            
            // Now create job in where an transition entity is created from all the capture data

            var ecb = endSimulationEcbSystem.CreateCommandBuffer();
            var job = Job;
            job.WithCode(() =>
            {
                Entity entity = ecb.CreateEntity(archetype);
                ecb.SetComponent(entity, new TransitionState());
            }).Run();
        }
    }
}