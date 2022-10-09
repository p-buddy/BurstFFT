using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct CurrentProjection: IComponentData, IValueSettable<Animation<float>>, IRequiredInArchetype
    {
        public Animation<float> Value { get; set; }
        public static implicit operator Animation<float>(CurrentProjection projection) => projection.Value;
    }
}