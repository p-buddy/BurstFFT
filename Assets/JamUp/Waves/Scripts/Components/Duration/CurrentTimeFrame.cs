using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public struct CurrentTimeFrame: IComponentData, IRequiredInArchetype
    {
        public float EndTime;
        public bool UpdateRequired(float time, float padding) => time + padding >= EndTime;
    }
}