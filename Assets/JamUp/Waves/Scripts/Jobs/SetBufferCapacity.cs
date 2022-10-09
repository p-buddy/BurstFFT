using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public struct SetBufferCapacity<TBufferElement>: IJob where TBufferElement : struct, IBufferElementData
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [ReadOnly]
        public NativeArray<Entity> Entity;
        public int SortKey;
        public int Capacity;
        
        public void Execute()
        {
            ECB.SetBuffer<TBufferElement>(SortKey, Entity[0]).EnsureCapacity(Capacity);
        }
    }
}