using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace JamUp.Waves.Scripts
{
    public struct SetAnimatableShaderFloatProperty<TCurrent> : IJobEntityBatch
        where TCurrent : struct, IComponentData, IValuable<Animation<float>>
    {
        public AnimatableShaderProperty<float> Property;
        
        public ComponentTypeHandle<PropertyBlockReference> PropertyBlockHandle;
        public ComponentTypeHandle<TCurrent> CurrentHandle;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<PropertyBlockReference> propertyBlocks = batchInChunk.GetNativeArray(PropertyBlockHandle);
            NativeArray<TCurrent> currents = batchInChunk.GetNativeArray(CurrentHandle);
            
            for(int i = 0; i < batchInChunk.Count; i++)
            {
                MaterialPropertyBlock block = (MaterialPropertyBlock)propertyBlocks[i].Handle.Target;
                Animation<float> value = currents[i].Value;
                
                block.SetFloat(Property.Animation.ID, (float)value.Curve);
                block.SetFloat(Property.From.ID, value.From);
                block.SetFloat(Property.To.ID, value.To);
            }
        }
    }
}