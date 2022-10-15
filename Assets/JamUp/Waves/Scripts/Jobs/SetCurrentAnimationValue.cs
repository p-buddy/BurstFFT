using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    [BurstCompile]
    public struct SetCurrentAnimationValue<TCurrent, TBuffer>: IJobEntityBatch
        where TCurrent : struct, IValueSettable<Animation<float>>, IComponentData
        where TBuffer : struct, IAnimatable, IValuable<float>, IBufferElementData
    {
        public ComponentTypeHandle<TCurrent> CurrentHandle;
        
        public BufferTypeHandle<TBuffer> BufferHandle;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<TCurrent> currents = batchInChunk.GetNativeArray(CurrentHandle);
            BufferAccessor<TBuffer> buffers = batchInChunk.GetBufferAccessor(BufferHandle);
            
            for (int i = 0; i < batchInChunk.Count; i++)
            {
                DynamicBuffer<TBuffer> buffer = buffers[i];
                
                TBuffer from = buffer[0];
                currents[i] = new TCurrent
                {
                    Value = new Animation<float>(from.Value, buffer[1].Value, from.AnimationCurve)
                };
                
                buffer.RemoveAt(0);
            }
        }
    }
}