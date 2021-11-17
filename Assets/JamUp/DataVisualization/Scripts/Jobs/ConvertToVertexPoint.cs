using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace JamUp.DataVisualization
{
    public struct ConvertToVertexPoint<TData, TConverter> : IJobParallelFor
        where TData : struct
        where TConverter : IConvertToWorldPoint<TData>
    {
        [ReadOnly]
        public NativeArray<TData> Data;

        [WriteOnly] 
        public NativeArray<float3> Vertices;
        
        [ReadOnly] 
        public TConverter Converter;

        public void Execute(int index)
        {
            Vertices[index] = Converter.Convert(Data[index], index);
        }
    }
}