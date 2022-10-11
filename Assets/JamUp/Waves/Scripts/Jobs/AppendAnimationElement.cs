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
        public EntityCommandBuffer ecb;
        [ReadOnly]
        public NativeArray<Entity> entity;
        public TBufferElement Element;
        public TProperty Property;
        
        public void Execute()
        {
            ecb.AppendToBuffer(entity[0], new TBufferElement
            {
                Value = Property.Value,
                AnimationCurve = Property.AnimationCurve
            });
        }
    }
}