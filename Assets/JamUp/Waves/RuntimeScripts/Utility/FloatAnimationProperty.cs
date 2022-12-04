using JamUp.Waves.RuntimeScripts.BufferIndexing;
using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Entities;
using Unity.Jobs;

namespace JamUp.Waves.RuntimeScripts
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
                                    in FloatAnimationProperties.UpdateInputs inputs)
            {
                job.BufferHandle = BufferHandle(inputs.System, true);
                job.CurrentHandle = ComponentHandle(inputs.System, false);
                job.IndexHandle = inputs.CurrentIndexHandle;
#if MULTITHREADED
                return job.ScheduleParallel(UpdateCurrentQuery, inputs.Dependency);
#else
                job.Run(UpdateCurrentQuery);
                return inputs.Dependency;
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