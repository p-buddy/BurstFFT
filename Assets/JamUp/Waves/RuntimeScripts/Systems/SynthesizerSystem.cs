using System;
using JamUp.Waves.RuntimeScripts.Audio;
using JamUp.Waves.RuntimeScripts.BufferIndexing;
using JamUp.Waves.RuntimeScripts.DSP;
using Unity.Audio;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.Waves.RuntimeScripts
{
    // Have a DSP Node for each wave?
    [UpdateBefore(typeof(SignalDrawerSystem))]
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
            graphs[index] = config.CreateGraph();
            var driver = new DefaultDSPGraphDriver {Graph = graphs[index]};
            driver.AttachToDefaultOutput();
            return index;
        }
        
        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            var localGraphs = graphs;

            Entities.WithAll<UpdateRequired>()
                    .WithNativeDisableParallelForRestriction(localGraphs)
                    .WithoutBurst()
                    .ForEach((ref DynamicBuffer<AudioKernelWrapper> nodes,
                              in AudioGraphReference reference,
                              in CurrentIndex index,
                              in CurrentWaveIndex waveIndex,
                              in DynamicBuffer<AllWavesElement> allWaves,
                              in DynamicBuffer<WaveCountElement> waveCounts) =>
                    {
                        var graph = localGraphs[reference.Index];
                        using var block = graph.CreateCommandBlock();
                        
                        AllWavesElement.Indexer indexer = new(index.Value, in waveCounts, in allWaves);
                        int waveCount = indexer.ComputedWaveCount;

                        while (nodes.Length < waveCount)
                        {
                            DSPNode node = PlayWaves.Create(block);
                            block.AddOutletPort(node, 2);
                            block.Connect(node, 0, graph.RootDSP, 0);
                            nodes.Add(new AudioKernelWrapper(node));
                        }

                        for (int i = 0; i < waveCount; i++)
                        {
                            indexer.GetWavesAt(waveIndex.Value, i, out var startingWave, out var endingWave);
                            PlayWaves.UpdateWaves updateTask = new(startingWave, endingWave);
                            PlayWaves.Update(block, nodes[i].Node, updateTask);
                        }

                        for (int i = 0; i < nodes.Length - waveCount; i++)
                        {
                            PlayWaves.Update(block, nodes[waveCount + i].Node, new PlayWaves.PauseWavesNode());
                        }
                    }).ScheduleParallel();
        }

        protected override void OnDestroy()
        {
            graphs.Dispose(Dependency);
            base.OnDestroy();
        }
        
        [BurstCompile(CompileSynchronously = true)]
        private struct PlayWaves: IAudioKernel<PlayWaves.Parameters, PlayWaves.Providers>
        {
            public bool Pause { get; set; }
            public AllWavesElement StartWave { get; set; }
            public AllWavesElement EndWave { get; set; }

            private float phase;
            public enum Parameters { }
            public enum Providers { }
            public void Initialize()
            {
            }

            public void Execute(ref ExecuteContext<Parameters, Providers> context)
            {
                if (Pause) return;
                
                //SampleBuffer input = context.Inputs.GetSampleBuffer(0);
                SampleBuffer output = context.Outputs.GetSampleBuffer(0);
                int channelCount = output.Channels;
                int sampleFrames = output.Samples;
                
                var delta = StartWave.Frequency * 200f / context.SampleRate;

                for (int frame = 0; frame < sampleFrames; ++frame)
                {
                    for (int channel = 0; channel < channelCount; ++channel)
                    {
                        NativeArray<float> channelBuffer = output.GetBuffer(channel);
                        channelBuffer[frame] = math.sin(phase * 2 * math.PI) * StartWave.Amplitude;
                    }
                    
                    phase += delta;
                    phase -= math.floor(phase);
                }
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
                private readonly AllWavesElement start;
                private readonly AllWavesElement end;

                public UpdateWaves(AllWavesElement start, AllWavesElement end)
                {
                    this.start = start;
                    this.end = end;
                }

                public void Update(ref PlayWaves audioKernel)
                {
                    audioKernel.StartWave = start;
                    audioKernel.EndWave = end;
                }
            }
            
            public struct PauseWavesNode: IAudioKernelUpdate<Parameters, Providers, PlayWaves>
            {
                public void Update(ref PlayWaves audioKernel)
                {
                    audioKernel.Pause = true;
                }
            }
        }

        
    }
}