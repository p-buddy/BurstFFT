using System;
using JamUp.Waves.RuntimeScripts.Audio;
using JamUp.Waves.RuntimeScripts.BufferIndexing;
using JamUp.Waves.RuntimeScripts.DSP;
using Unity.Audio;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.Waves.RuntimeScripts
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SignalManagementSystem))]
    public partial class SynthesizerSystem: SystemBase
    {
        private const int MaxGraphs = 100;
        private readonly GraphConfig config = GraphConfig.Init();
        private NativeArray<DSPGraph> graphs = new (MaxGraphs, Allocator.Persistent);

        private int lastIndex = -1;

        public int GetGraphReference()
        {
            int index = ++lastIndex;
            if (index >= MaxGraphs - 1) throw new Exception("Uh oh, too many"); // is this really the best way?
            DSPGraph graph = config.CreateGraph();
            var driver = new DefaultDSPGraphDriver {Graph = graph};
            driver.AttachToDefaultOutput();
            graphs[index] = graph;
            return index;
        }
        
        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            var localGraphs = graphs;

#if MULTITHREADED
            JobHandle handle = Entities
#else
            JobHandle handle = Dependency.CompleteAndGetBack();
            Entities
#endif               
                .WithAll<UpdateRequired>()
                .WithNativeDisableParallelForRestriction(localGraphs)
                .WithoutBurst()
                .ForEach((ref DynamicBuffer<AudioKernelWrapper> nodes,
                          in AudioGraphReference reference,
                          in CurrentTimeFrame timeFrame,
                          in SignalEntity signal,
                          in DynamicBuffer<CurrentWavesElement> currentWaves) =>
                {
                    var graph = localGraphs[reference.Index];

                    using var block = graph.CreateCommandBlock();
                        
                    int waveCount = currentWaves.Length;

                    float frequency = signal.RootFrequency;
                    float duration = timeFrame.EndTime - timeFrame.StartTime;

                    while (nodes.Length < waveCount)
                    {
                        DSPNode node = PlayWaves.Create(block);
                        block.AddOutletPort(node, 2);
                        block.Connect(node, 0, graph.RootDSP, 0);
                        nodes.Add(new AudioKernelWrapper(node));
                    }

                    for (int i = 0; i < waveCount; i++)
                    {
                        PlayWaves.UpdateWaves updateTask = new(currentWaves[i], frequency, duration);
                        PlayWaves.Update(block, nodes[i].Node, updateTask);
                    }

                    for (int i = 0; i < nodes.Length - waveCount; i++)
                    {
                        PlayWaves.Update(block, nodes[waveCount + i].Node, new PlayWaves.PauseWavesNode());
                    }
                })
#if MULTITHREADED
                .ScheduleParallel(Dependency);
#else
                .Run();
#endif
            Dependency = handle;
        }

        protected override void OnDestroy()
        {
            graphs.Dispose(Dependency);
            base.OnDestroy();
        }

        //[BurstCompile(CompileSynchronously = true)]
        private struct PlayWaves: IAudioKernel<PlayWaves.Parameters, PlayWaves.Providers>
        {
            private struct Setting
            {
                private readonly float duration;
                private readonly float rootFrequency;
                private CurrentWavesElement wave;

                private int framesInDuration;
                private float frequencyFactor;
                private float frameTime;
                private int elapsedFrames;
                private float phase;
                private bool initComplete;
                private bool valid;

                public Setting(CurrentWavesElement wave, float currentFreq, float currentDuration)
                {
                    valid = true;
                    elapsedFrames = 0;
                    phase = wave.Phase.x / (2 * math.PI);
                    this.wave = wave;
                    rootFrequency = currentFreq;
                    duration = currentDuration;
                    initComplete = false;
                     
                    framesInDuration = default;
                    frameTime = default;
                    frequencyFactor = default;
                }

                private void Init(int sampleRate)
                {
                    if (initComplete) return;
                    
                    frameTime = 1f / sampleRate;
                    framesInDuration = (int)math.ceil(duration * sampleRate);
                    frequencyFactor = rootFrequency * frameTime;
                    initComplete = true;
                }
                
                public float GetMono(int sampleRate)
                {
                    if (!valid) return 0f;
                    
                    Init(sampleRate);

                    float lerpTime = (float)elapsedFrames / framesInDuration;
                    float value = wave.ValueAtPhase(phase * 2 * math.PI, lerpTime);

                    phase += wave.LerpFrequency(lerpTime) * frequencyFactor + wave.PhaseDelta(lerpTime, frameTime);
                    phase -= math.floor(phase);
                    elapsedFrames++;

                    return value;
                }
            }

            private const int SmoothInSampleCount = 1000;

            private Setting current;
            private Setting previous;

            private bool Pause { get; set; }

            private CurrentWavesElement wave;

            public enum Parameters { }
            public enum Providers { }
            
            public void Initialize()
            {
            }

            public void Execute(ref ExecuteContext<Parameters, Providers> context)
            {
                if (Pause) return;
                
                SampleBuffer output = context.Outputs.GetSampleBuffer(0);
                int channelCount = output.Channels;
                int sampleFrames = output.Samples;

                int sampleRate = context.SampleRate;
                float framesReciprocal = 1f / SmoothInSampleCount;
                
                for (int frame = 0; frame < sampleFrames; frame++)
                {
                    float smoothingFactor = frame * framesReciprocal;
                    float value = frame > SmoothInSampleCount
                        ? current.GetMono(sampleRate)
                        : math.lerp(previous.GetMono(sampleRate), current.GetMono(sampleRate), smoothingFactor);

                    for (int channel = 0; channel < channelCount; ++channel)
                    {
                        NativeArray<float> channelBuffer = output.GetBuffer(channel);
                        channelBuffer[frame] = value;
                    }
                }

                previous = current;
            }

            private void SetWave(CurrentWavesElement currentWave, float currentFreq, float currentDuration)
            {
                previous = current;
                current = new Setting(currentWave, currentFreq, currentDuration);
            }

            public void Dispose()
            {
            }

            public static void Update<TAudioKernelUpdate>(DSPCommandBlock block,
                                                          DSPNode node,
                                                          TAudioKernelUpdate task)
                where TAudioKernelUpdate : struct, IAudioKernelUpdate<Parameters, Providers, PlayWaves> =>
                block.UpdateAudioKernel<TAudioKernelUpdate, Parameters, Providers, PlayWaves>(task, node);

            public static DSPNode Create(DSPCommandBlock block) => block.CreateDSPNode<Parameters, Providers, PlayWaves>();
            
            public readonly struct UpdateWaves: IAudioKernelUpdate<Parameters, Providers, PlayWaves>
            {
                private readonly CurrentWavesElement wave;
                private readonly float rootFrequency;
                private readonly float duration;

                public UpdateWaves(CurrentWavesElement wave, float rootFrequency, float duration)
                {
                    this.wave = wave;
                    this.rootFrequency = rootFrequency;
                    this.duration = duration;
                } 

                public void Update(ref PlayWaves audioKernel)
                {
                    audioKernel.SetWave(wave, rootFrequency, duration);
                }
            }
            
            public struct PauseWavesNode: IAudioKernelUpdate<Parameters, Providers, PlayWaves>
            {
                public void Update(ref PlayWaves audioKernel) => audioKernel.Pause = true;
            }
        }

        
    }
}