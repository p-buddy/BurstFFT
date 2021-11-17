using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace JamUp.DataVisualization
{
    public struct BuildTrianglesFromCuboidVertices : IJobParallelFor
    {
        private const int SuccessorFaceVertexIndexOffset = 4;
        private const int SuccessorTriangleIndexOffset = 3;
        private const int TrianglesPerVertexIndex = 2;
        
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<int> Triangles;
        
        [ReadOnly]
        public int VertexLength;
        
        public void Execute(int vertexIndex)
        {
            if (vertexIndex >= VertexLength - 4)
            {
                return;
            }
            
            BuildTriangleWithEdgeOnThisFace(vertexIndex); // normals are correct
            BuildTriangleWithEdgeOnNextFace(vertexIndex); // normals are incorrect
        }

        private int GetPredecessorIndexInFace(int index) => index % 4 > 0 ? index - 1 : index + 3;
        private int GetTriangleIndex(int vertexIndex) => vertexIndex * TrianglesPerVertexIndex * SuccessorTriangleIndexOffset;

        private void BuildTriangleWithEdgeOnThisFace(int vertexIndex)
        {
            int indexInTriangles = GetTriangleIndex(vertexIndex);
            Triangles[indexInTriangles] = vertexIndex;
            Triangles[indexInTriangles + 1] = GetPredecessorIndexInFace(vertexIndex);
            Triangles[indexInTriangles + 2] = GetPredecessorIndexInFace(vertexIndex) + SuccessorFaceVertexIndexOffset;
        }
        
        private void BuildTriangleWithEdgeOnNextFace(int vertexIndex)
        {
            int indexInTriangles = GetTriangleIndex(vertexIndex) + SuccessorTriangleIndexOffset;
            Triangles[indexInTriangles + 2] = vertexIndex;
            Triangles[indexInTriangles + 1] = vertexIndex + SuccessorFaceVertexIndexOffset;
            Triangles[indexInTriangles] = GetPredecessorIndexInFace(vertexIndex) + SuccessorFaceVertexIndexOffset;
        }
    }
}