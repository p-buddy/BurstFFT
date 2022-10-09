using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public struct AppendAnimationElement<TData, TProperty, TBufferElement>: IJob 
        where TData : new()
        where TProperty : struct, IValuable<TData>, IAnimatable
        where TBufferElement : struct, IBufferElementData, IAnimatableSettable, IValueSettable<TData>
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly]
        public NativeArray<Entity> entity;
        public int SortKey;
        public TBufferElement Element;
        public TProperty Property;
        
        public void Execute()
        {
            ecb.AppendToBuffer(SortKey, entity[0], new TBufferElement
            {
                Value = Property.Value,
                AnimationCurve = Property.AnimationCurve
            });
        }
    }
}