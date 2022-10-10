using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Entities;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public readonly struct FloatAnimationProperty<TComponent, TBuffer>: IFloatAnimationProperty
            where TComponent : struct, IComponentData, IValueSettable<Animation<float>>
            where TBuffer : struct, IBufferElementData, IAnimatable, IValuable<float>
        {
            private EntityQuery UpdateCurrentQuery { get; }
            private EntityQuery SetShaderPropertyQuery { get; }
            private AnimatableShaderProperty<float> ShaderProperty { get; }

            public FloatAnimationProperty(string propertyName)
            {
                UpdateCurrentQuery = new EntityQueryBuilder().WithAllReadOnly<UpdateRequired, TComponent>()
                                                             .WithAll<TBuffer>()
                                                             .ToEntityQuery();
                
                SetShaderPropertyQuery = new EntityQueryBuilder()
                                         .WithAllReadOnly<UpdateRequired, PropertyBlockReference, TComponent>()
                                         .ToEntityQuery();

                ShaderProperty = new AnimatableShaderProperty<float>(propertyName);
            }

            public JobHandle UpdateCurrent(ComponentSystemBase system, JobHandle dependency)
            {
                return new SetCurrentAnimationValue<float, TComponent, TBuffer>
                {
                    BufferHandle = BufferHandle(system, false),
                    CurrentHandle = ComponentHandle(system, false)
                }.ScheduleParallel(UpdateCurrentQuery, dependency);
            }

            public JobHandle SetBlock(ComponentSystemBase system,
                                      JobHandle dependency,
                                      ComponentTypeHandle<PropertyBlockReference> propertyBlockHandle)
            {
                return new SetAnimatableShaderFloatProperty<TComponent>
                {
                    Property = ShaderProperty,
                    PropertyBlockHandle = propertyBlockHandle,
                    CurrentHandle = ComponentHandle(system, true)
                }.ScheduleParallel(SetShaderPropertyQuery, dependency);
            }

            private static ComponentTypeHandle<TComponent> ComponentHandle(ComponentSystemBase system, bool readOnly) =>
                system.GetComponentTypeHandle<TComponent>(readOnly);

            private static BufferTypeHandle<TBuffer> BufferHandle(ComponentSystemBase system, bool readOnly) =>
                system.GetBufferTypeHandle<TBuffer>(readOnly);
        }
        
}