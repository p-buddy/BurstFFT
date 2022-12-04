using JamUp.Waves.RuntimeScripts.BufferIndexing;
using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace JamUp.Waves.RuntimeScripts
{
    [BurstCompile]
    public struct SetCurrentAnimationValue<TCurrent, TBuffer>: IJobEntityBatch
        where TCurrent : struct, IValueSettable<Animation<float>>, IComponentData
        where TBuffer : struct, IAnimatable, IValuable<float>, IBufferElementData
    {
        public ComponentTypeHandle<TCurrent> CurrentHandle;
        
        [ReadOnly]
        public ComponentTypeHandle<CurrentIndex> IndexHandle;
        
        [ReadOnly]
        public BufferTypeHandle<TBuffer> BufferHandle;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<TCurrent> currents = batchInChunk.GetNativeArray(CurrentHandle);
            NativeArray<CurrentIndex> indices = batchInChunk.GetNativeArray(IndexHandle);
            BufferAccessor<TBuffer> buffers = batchInChunk.GetBufferAccessor(BufferHandle);
            
            for (int i = 0; i < batchInChunk.Count; i++)
            {
                DynamicBuffer<TBuffer> buffer = buffers[i];
                
                TBuffer from = buffer[indices[i].Value];
                currents[i] = new TCurrent
                {
                    Value = new Animation<float>(from.Value, buffer[1].Value, from.AnimationCurve)
                };
            }
        }
    }
}