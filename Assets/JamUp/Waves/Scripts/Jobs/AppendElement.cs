using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public struct AppendElement<TData, TBufferElement> : IJob
        where TBufferElement : struct, IBufferElementData, IValueSettable<TData> where TData : new()
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly]
        public NativeArray<Entity> entity;
        public int SortKey;
        public TData Data;
        
        public void Execute()
        {
            ecb.AppendToBuffer(SortKey, entity[0], new TBufferElement
            {
                Value = Data
            });
        }
    }
}