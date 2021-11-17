using Unity.Collections;
using Unity.Jobs;

namespace JamUp.DataVisualization
{
    public struct BuildContiguousIndices : IJobParallelFor
    {
        [WriteOnly]
        public NativeArray<int> Indices;
        
        public void Execute(int index)
        {
            Indices[index] = index;
        }
    }
}