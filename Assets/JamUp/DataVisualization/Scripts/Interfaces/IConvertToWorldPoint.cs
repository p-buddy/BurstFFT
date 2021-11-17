using Unity.Mathematics;

namespace JamUp.DataVisualization
{
    public interface IConvertToWorldPoint<TInput>
    {
        float3 Convert(TInput value, int index);
    }
}