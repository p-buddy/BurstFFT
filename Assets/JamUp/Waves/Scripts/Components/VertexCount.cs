using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct VertexCount: IComponentData, IRequiredInArchetype
    {
        public int Value;
    }
}