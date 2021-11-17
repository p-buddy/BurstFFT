using Unity.Mathematics;

namespace JamUp.DataVisualization
{
    public readonly struct ScaleAndOffsetPoint : IConvertToWorldPoint<float3>
    {
        public float3 Offset { get; }
        public float Length { get; }
        
        public ScaleAndOffsetPoint(float3 offset, float length)
        {
            Offset = offset;
            Length = length;
        }
        
        public float3 Convert(float3 value, int index)
        {
            return Offset + value * Length;
        }
    }
}