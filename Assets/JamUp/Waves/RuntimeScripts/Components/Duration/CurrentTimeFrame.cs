using Unity.Entities;
using Unity.Mathematics;

namespace JamUp.Waves.RuntimeScripts
{
    public struct CurrentTimeFrame: IComponentData, IRequiredInArchetype
    {
        public float StartTime;

        public float EndTime;
        public bool UpdateRequired(float time, float padding) => time + padding >= EndTime;
        public float Interpolate(float now) => math.min((now - StartTime) / (EndTime - StartTime), 1f) ;
    }
}