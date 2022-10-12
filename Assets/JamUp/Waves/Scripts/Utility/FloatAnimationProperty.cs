using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Entities;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public readonly struct FloatAnimationProperty<TComponent, TBuffer>
            where TComponent : struct, IComponentData, IValueSettable<Animation<float>>
            where TBuffer : struct, IBufferElementData, IAnimatable, IValuable<float>
    {
            public EntityQuery UpdateCurrentQuery { get; }
            public EntityQuery SetShaderPropertyQuery { get; }
            public AnimatableShaderProperty<float> ShaderProperty { get; }

            public FloatAnimationProperty(string propertyName)
            {
                EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
                EntityQueryDesc updateDesc = new EntityQueryBuilder()
                                             .WithAllReadOnly<UpdateRequired, TComponent>()
                                             .WithAll<TBuffer>()
                                             .ToEntityQueryDesc();
                
                UpdateCurrentQuery = manager.CreateEntityQuery(updateDesc);

                EntityQueryDesc setDesc = new EntityQueryBuilder()
                                          .WithAllReadOnly<UpdateRequired, PropertyBlockReference, TComponent>()
                                          .ToEntityQueryDesc();
                
                SetShaderPropertyQuery = manager.CreateEntityQuery(setDesc);

                ShaderProperty = new AnimatableShaderProperty<float>(propertyName);
            }

            public JobHandle Update(SetCurrentAnimationValue<TComponent, TBuffer> job,
                                    ComponentSystemBase system,
                                    JobHandle dependency)
            {
                job.BufferHandle = BufferHandle(system, false);
                job.CurrentHandle = ComponentHandle(system, false);
#if MULTITHREADED
                return job.ScheduleParallel(UpdateCurrentQuery, dependency);
#else
                job.Run(UpdateCurrentQuery);
                return dependency;
#endif
            }

            public JobHandle Set(SetAnimatableShaderFloatProperty<TComponent> job,
                                 ComponentSystemBase system,
                                 ComponentTypeHandle<PropertyBlockReference> propertyBlockHandle,
                                 JobHandle dependency)
            {
                job.Property = ShaderProperty;
                job.PropertyBlockHandle = propertyBlockHandle;
                job.CurrentHandle = ComponentHandle(system, true);
#if MULTITHREADED
                return job.ScheduleParallel(SetShaderPropertyQuery, dependency);
#else
                job.Run(SetShaderPropertyQuery);
                return dependency;
#endif
            }

            public ComponentTypeHandle<TComponent> ComponentHandle(ComponentSystemBase system, bool readOnly) =>
                system.GetComponentTypeHandle<TComponent>(readOnly);

            public BufferTypeHandle<TBuffer> BufferHandle(ComponentSystemBase system, bool readOnly) =>
                system.GetBufferTypeHandle<TBuffer>(readOnly);
        }
        
}