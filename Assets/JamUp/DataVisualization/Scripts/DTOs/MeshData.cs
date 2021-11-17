using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace JamUp.DataVisualization
{
    public readonly struct MeshData : IDisposable
    {
        public NativeArray<float3> Vertices { get; }
        public NativeArray<float3> Normals { get; }
        public NativeArray<int> Triangles { get; }
        public JobHandle VerticesAndNormalsHandle { get; }
        public JobHandle TrianglesHandle { get; }
        public JobHandle CombinedHandle { get; }
        public MeshData(NativeArray<float3> vertices,
                        NativeArray<float3> normals,
                        NativeArray<int> triangles,
                        JobHandle verticesAndNormalsHandle,
                        JobHandle trianglesHandle)
        {
            Vertices = vertices;
            Normals = normals;
            Triangles = triangles;
            VerticesAndNormalsHandle = verticesAndNormalsHandle;
            TrianglesHandle = trianglesHandle;
            CombinedHandle = JobHandle.CombineDependencies(VerticesAndNormalsHandle, TrianglesHandle);
        }

        public void Dispose()
        {
            Vertices.Dispose();
            Normals.Dispose();
            Triangles.Dispose();
        }
            
        public void Dispose(JobHandle handle)
        {
            Vertices.Dispose();
            Normals.Dispose();
            Triangles.Dispose();
        }
    }
}