using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct SetCurrentAnimationValue<TType, TCurrent, TBuffer>: IJobEntityBatch
        where TType : new()
        where TCurrent : struct, IValueSettable<Animation<TType>>, IComponentData
        where TBuffer : struct, IAnimatable, IValuable<TType>, IBufferElementData
    {
        public ComponentTypeHandle<TCurrent> CurrentHandle;
        public BufferTypeHandle<TBuffer> BufferHandle;

        [BurstCompile]
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<TCurrent> currents = batchInChunk.GetNativeArray(CurrentHandle);
            BufferAccessor<TBuffer> buffers = batchInChunk.GetBufferAccessor(BufferHandle);
            
            for (int i = 0; i < batchInChunk.Count; i++)
            {
                TCurrent current = currents[i];
                DynamicBuffer<TBuffer> buffer = buffers[i];
                
                TBuffer from = buffer[0];
                current.Value = new Animation<TType>(from.Value, buffer[1].Value, from.AnimationCurve);
                
                buffer.RemoveAt(0);
            }
        }
    }
}