using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public static class FloatAnimationProperties
    {
        public enum Kind
        {
            Projection,
            Thickness,
            SignalLength,
            SampleRate,
        }

        private static readonly Dictionary<Kind, IFloatAnimationProperty> Properties = new()
        {
            { Kind.Projection, new FloatAnimationProperty<CurrentProjection, ProjectionElement>(nameof(Kind.Projection)) },
            { Kind.Thickness, new FloatAnimationProperty<CurrentThickness, ThicknessElement>(nameof(Kind.Thickness)) },
            { Kind.SignalLength, new FloatAnimationProperty<CurrentSignalLength, SignalLengthElement>(nameof(Kind.SignalLength)) },
            { Kind.SampleRate, new FloatAnimationProperty<CurrentSampleRate, SampleRateElement>(nameof(Kind.SampleRate)) },
        };
        
        private static readonly Dictionary<Kind, JobHandle> UpdateCurrentHandles = new()
        {
            { Kind.Projection, default },
            { Kind.Thickness, default },
            { Kind.SignalLength, default },
            { Kind.SampleRate, default },
        };

        public static void UpdateAll(JobHandle dependency, ComponentSystemBase system)
        {
            foreach ((Kind kind, IFloatAnimationProperty property) in Properties)
            {
                UpdateCurrentHandles[kind] = property.UpdateCurrent(system, dependency);
            }
        }
        
        public static JobHandle SetAll(ComponentSystemBase system)
        {
            JobHandle combinedHandle = default;
            foreach ((Kind kind, IFloatAnimationProperty property) in Properties)
            {
                JobHandle handle = property.SetBlock(system, 
                                                     UpdateCurrentHandles[kind],
                                                     system.GetComponentTypeHandle<PropertyBlockReference>());
                combinedHandle = JobHandle.CombineDependencies(combinedHandle, handle);
            }

            return combinedHandle;
        }
        
        public static JobHandle GetUpdateHandle(Kind a, Kind b) =>
            JobHandle.CombineDependencies(UpdateCurrentHandles[a], UpdateCurrentHandles[b]);

    }
}