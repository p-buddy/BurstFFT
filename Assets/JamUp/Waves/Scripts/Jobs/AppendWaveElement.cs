using JamUp.Waves.Scripts.API;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public struct AppendWaveElement: IJob
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [ReadOnly]
        public NativeArray<Entity> Entity;
        
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<Animatable<WaveState>> WaveStates;
        
        public int SortKey;
        
        public void Execute()
        {
            Entity ent = Entity[0];
            for (int i = 0; i < WaveStates.Length; i++)
            {
                Animatable<WaveState> waveState = WaveStates[i];
                ECB.AppendToBuffer(SortKey + i, ent, new AllWavesElement { Value = waveState });
            }
        }
    }
}