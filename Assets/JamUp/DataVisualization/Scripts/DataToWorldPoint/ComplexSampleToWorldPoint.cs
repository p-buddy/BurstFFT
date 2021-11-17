using JamUp.Math;
using Unity.Mathematics;

namespace JamUp.DataVisualization
{
    public struct ComplexSampleToWorldPoint : IConvertToWorldPoint<Complex>
    { 
        public int SampleRate { get; }
        public float LateralOffset { get; }
        
        public ComplexSampleToWorldPoint(int sampleRate, float lateralOffset = 0f)
        {
            SampleRate = sampleRate;
            LateralOffset = lateralOffset;
        }
        
        public float3 Convert(Complex value, int index)
        {
            float time = (float)index / SampleRate;
            return math.right() * value.Real + math.up() * value.Imaginary + math.right() * LateralOffset + math.forward() * time;        
        }
    }
}