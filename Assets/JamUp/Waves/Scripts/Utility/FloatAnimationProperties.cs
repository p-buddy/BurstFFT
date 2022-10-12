using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public static class FloatAnimationProperties
    {
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
            UpdateHandles[Kind.Projection] = Projection.Update(UpdateCurrent.Projection, system, dependency);
            UpdateHandles[Kind.Thickness] = Thickness.Update(UpdateCurrent.Thickness, system, dependency);
            UpdateHandles[Kind.SampleRate] = SampleRate.Update(UpdateCurrent.SampleRate, system, dependency);
            UpdateHandles[Kind.SignalLength] = SignalLength.Update(UpdateCurrent.SignalLength, system, dependency);
        }

        public static JobHandle SetAll(ComponentSystemBase system)
        {
            NativeArray<JobHandle> handles = new NativeArray<JobHandle>(4, Allocator.Temp);
            ComponentTypeHandle<PropertyBlockReference> blockHandle = system.GetComponentTypeHandle<PropertyBlockReference>();

            handles[0] = Projection.Set(SetProperty.Projection, system, blockHandle, UpdateHandles[Kind.Projection]);
            handles[1] = Thickness.Set(SetProperty.Thickness, system, blockHandle, UpdateHandles[Kind.Thickness]);
            handles[2] = SampleRate.Set(SetProperty.SampleRate, system, blockHandle, UpdateHandles[Kind.SampleRate]);
            handles[3] = SignalLength.Set(SetProperty.SignalLength, system, blockHandle, UpdateHandles[Kind.SignalLength]);
            
            JobHandle combined = JobHandle.CombineDependencies(handles);
            handles.Dispose();
            return combined;
        }
        
        public static JobHandle GetUpdateHandle(Kind a, Kind b) =>
            JobHandle.CombineDependencies(UpdateHandles[a], UpdateHandles[b]);

    }
}