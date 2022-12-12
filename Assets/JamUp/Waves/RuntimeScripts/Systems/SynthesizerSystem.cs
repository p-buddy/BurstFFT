using System;
using JamUp.Waves.RuntimeScripts.Audio;
using JamUp.Waves.RuntimeScripts.BufferIndexing;
using JamUp.Waves.RuntimeScripts.DSP;
using JamUp.Waves.RuntimeScripts.Synth;
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
                          in DynamicBuffer<CurrentWavesElement> currentWaves,
                          in DynamicBuffer<CurrentWaveAxes> currentAxes) =>
                {
                    var graph = localGraphs[reference.Index];

                    using var block = graph.CreateCommandBlock();
                        
                    int waveCount = currentWaves.Length;

                    float frequency = signal.RootFrequency;
                    float duration = timeFrame.EndTime - timeFrame.StartTime;

                    while (nodes.Length < waveCount)
                    {
                        DSPNode node = SynthKernel.Create(block);
                        block.AddOutletPort(node, 2);
                        block.Connect(node, 0, graph.RootDSP, 0);
                        nodes.Add(new AudioKernelWrapper(node));
                    }

                    for (int i = 0; i < waveCount; i++)
                    {
                        SynthKernel.Set set = new(currentWaves[i], currentAxes[i], frequency, duration);
                        SynthKernel.Update(block, nodes[i].Node, set);
                    }

                    for (int i = 0; i < nodes.Length - waveCount; i++)
                    {
                        SynthKernel.Update<SynthKernel.Pause>(block, nodes[waveCount + i].Node, default);
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
    }
}