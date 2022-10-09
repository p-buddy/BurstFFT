using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct CurrentThickness: IComponentData, IValueSettable<Animation<float>>, IRequiredInArchetype
    {
        public Animation<float> Value { get; set; }
        public static implicit operator Animation<float>(CurrentThickness projection) => projection.Value;
    }
}