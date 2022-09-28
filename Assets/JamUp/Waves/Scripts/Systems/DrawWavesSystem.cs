using JamUp.Waves.Scripts.API;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace JamUp.Waves.Scripts.Systems
{
    public partial class DrawWavesSystem: SystemBase
    {
        private EntityArchetype archetype;
        private EndSimulationEntityCommandBufferSystem endSimulationEcbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            archetype = EntityManager.CreateArchetype(typeof(TransitionState));
            endSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            Entities.ForEach(() =>
            {
                
            }).Run();
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