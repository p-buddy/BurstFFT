using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public struct AppendElement<TData, TBufferElement> : IJob
        where TBufferElement : struct, IBufferElementData, IValueSettable<TData> where TData : new()
    {
        public EntityCommandBuffer ecb;
        [ReadOnly]
        public NativeArray<Entity> entity;
        public TData Data;
        
        public void Execute()
        {
            ecb.AppendToBuffer(entity[0], new TBufferElement
            {
                Value = Data
            });
        }
    }
}