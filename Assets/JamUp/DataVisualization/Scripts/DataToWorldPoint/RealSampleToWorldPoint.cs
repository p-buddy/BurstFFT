using Unity.Mathematics;

namespace JamUp.DataVisualization
{
    public struct RealSampleToWorldPoint : IConvertToWorldPoint<float>
    { 
        public int SampleRate { get; }
        public float LateralOffset { get; }
        
        public float3 DisplacementAxis { get; }
        public float3 PropagationAxis { get; }
        public float3 OffsetAxis { get; }
        
        public RealSampleToWorldPoint(int sampleRate,
                                      float3 displacementAxis,
                                      float3 propagationAxis,
                                      float3 offsetAxis = default,
                                      float lateralOffset = 0f)
        {
            SampleRate = sampleRate;
            DisplacementAxis = displacementAxis;
            PropagationAxis = propagationAxis;
            OffsetAxis = offsetAxis;
            LateralOffset = lateralOffset;
        }
        
        public float3 Convert(float value, int index)
        {
            float time = (float)index / SampleRate;
            return DisplacementAxis * value + OffsetAxis * LateralOffset + PropagationAxis * time;
        }
    }
}