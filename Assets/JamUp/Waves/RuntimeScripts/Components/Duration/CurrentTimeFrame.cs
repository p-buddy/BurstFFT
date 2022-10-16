using Unity.Entities;

namespace JamUp.Waves.RuntimeScripts
{
    public struct CurrentTimeFrame: IComponentData, IRequiredInArchetype
    {
        public float StartTime;

        public float EndTime;
        public bool UpdateRequired(float time, float padding) => time + padding >= EndTime;
        public float Interpolant(float now) => (now - StartTime) / (EndTime - StartTime) ;
    }
}