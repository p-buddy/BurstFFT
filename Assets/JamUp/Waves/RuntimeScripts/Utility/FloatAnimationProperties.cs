using System.Collections.Generic;
using JamUp.Waves.RuntimeScripts.BufferIndexing;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace JamUp.Waves.RuntimeScripts
{
    public static class FloatAnimationProperties
    {
        public readonly struct UpdateInputs
        {
            public ComponentSystemBase System { get; }
            public JobHandle Dependency { get; }
            public ComponentTypeHandle<CurrentIndex> CurrentIndexHandle { get; }

            public UpdateInputs(ComponentSystemBase system, JobHandle dependency)
            {
                System = system;
                Dependency = dependency;
                CurrentIndexHandle = system.GetComponentTypeHandle<CurrentIndex>(true);
            }
        }
        
        private static class UpdateCurrent
        {
            public static SetCurrentAnimationValue<CurrentProjection, ProjectionElement> Projection => new();
            public static SetCurrentAnimationValue<CurrentThickness, ThicknessElement> Thickness => new();
            public static SetCurrentAnimationValue<CurrentSignalLength, SignalLengthElement> SignalLength => new ();
            public static SetCurrentAnimationValue<CurrentSampleRate, SampleRateElement> SampleRate => new();
        }
        
        private static class SetProperty
        {
            public static SetAnimatableShaderFloatProperty<CurrentProjection> Projection => new();
            public static SetAnimatableShaderFloatProperty<CurrentThickness> Thickness => new();
            public static SetAnimatableShaderFloatProperty<CurrentSignalLength> SignalLength => new ();
            public static SetAnimatableShaderFloatProperty<CurrentSampleRate> SampleRate => new();
        }
        
        public enum Kind
        {
            Projection,
            Thickness,
            SignalLength,
            SampleRate,
        }

        private static readonly FloatAnimationProperty<CurrentProjection, ProjectionElement> Projection 
            = new(nameof(Kind.Projection));
        private static readonly FloatAnimationProperty<CurrentThickness, ThicknessElement> Thickness 
            = new(nameof(Kind.Thickness));
        private static readonly FloatAnimationProperty<CurrentSignalLength, SignalLengthElement> SignalLength 
            = new(nameof(Kind.SignalLength));
        private static readonly FloatAnimationProperty<CurrentSampleRate, SampleRateElement> SampleRate 
            = new(nameof(Kind.SampleRate));

        private static readonly Dictionary<Kind, JobHandle> UpdateHandles = new()
        {
            { Kind.Projection, default },
            { Kind.Thickness, default },
            { Kind.SignalLength, default },
            { Kind.SampleRate, default },
        };

        public static void UpdateAll(JobHandle dependency, ComponentSystemBase system)
        {
            UpdateInputs inputs = new (system, dependency);
            UpdateHandles[Kind.Projection] = Projection.Update(UpdateCurrent.Projection, in inputs);
            UpdateHandles[Kind.Thickness] = Thickness.Update(UpdateCurrent.Thickness, in inputs);
            UpdateHandles[Kind.SampleRate] = SampleRate.Update(UpdateCurrent.SampleRate, in inputs);
            UpdateHandles[Kind.SignalLength] = SignalLength.Update(UpdateCurrent.SignalLength, in inputs);
        }

        public static JobHandle SetAll(ComponentSystemBase system, JobHandle dependency)
        {
            static JobHandle Dependency(Kind kind, JobHandle dependency) => JobHandle.CombineDependencies(dependency, UpdateHandles[kind]);
            
            ComponentTypeHandle<PropertyBlockReference> blockRef = system.GetComponentTypeHandle<PropertyBlockReference>();

            JobHandle handle = Projection.Set(SetProperty.Projection,
                                              system,
                                              blockRef,
                                              Dependency(Kind.Projection, dependency));
            handle = Thickness.Set(SetProperty.Thickness, system, blockRef, Dependency(Kind.Thickness, handle));
            handle = SampleRate.Set(SetProperty.SampleRate, system, blockRef, Dependency(Kind.SampleRate, handle));
            handle = SignalLength.Set(SetProperty.SignalLength, system, blockRef, Dependency(Kind.SignalLength, handle));

            return handle;
        }
        
        public static JobHandle GetUpdateHandle(Kind a, Kind b) =>
            JobHandle.CombineDependencies(UpdateHandles[a], UpdateHandles[b]);

    }
}