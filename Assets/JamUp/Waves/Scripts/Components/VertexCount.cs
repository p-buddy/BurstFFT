using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct VertexCount: IComponentData, IValueSettable<int>, IRequiredInArchetype
    {
        public int Value { get; set; }
    }
}