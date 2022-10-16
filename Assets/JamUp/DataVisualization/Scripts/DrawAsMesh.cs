using System.Collections.Generic;
using JamUp.DataVisualization.Waves;
using JamUp.Math;
using JamUp.UnityUtility;
using JamUp.Waves.RuntimeScripts;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.DataVisualization
{
    public static class DrawAsMesh
    {
        private const int BatchCount = 64;
        private static Queue<GameObject> GameObjectPool = new Queue<GameObject>();
        private static Material LineMaterial => MaterialLibrary.GetMaterial("SolidColor3D");

        #region Drawing Tasks
        public static void RealWave(in Wave wave, float time, int sampleRate, Color color, float thickness)
        {
            ObjectForMeshDrawing objectForDrawing = GetObjectForDrawing();
            WaveDrawingState initialState = WaveDrawingState.ForReal(sampleRate, thickness, time, color, wave);
            objectForDrawing.Attach<LiveWaveDrawer, WaveDrawingState>().SetInitialState(initialState);
        }
        
        public static void ComplexWave(in Wave wave, float time, int sampleRate, Color color, float thickness)
        {
            int length = (int)(sampleRate * time);
            using NativeArray<Complex> data = WaveDataFactory.GetComplexValueArray(in wave,
                                                                                   length,
                                                                                   sampleRate,
                                                                                   Allocator.Persistent,
                                                                                   out JobHandle handle);
        }

        public static void Axes3D(float length, Vector3 offset, float thickness)
        {
            float3[] endPoints = {math.right(), -math.right(), math.up(), -math.up(), math.forward(), -math.forward()};
            using var axisPoints = new NativeArray<float3>(endPoints, Allocator.TempJob);

            var scaleAndOffsetPoint = new ScaleAndOffsetPoint(offset, length);
            Draw(axisPoints.GetSubArray(0, 2), in scaleAndOffsetPoint, Color.red, thickness);
            Draw(axisPoints.GetSubArray(2, 2), in scaleAndOffsetPoint, Color.green, thickness);
            Draw(axisPoints.GetSubArray(4, 2), in scaleAndOffsetPoint, Color.blue, thickness);
        }
        
        public static void Line(Vector3[] points, float thickness, Color color, Vector3 offset = default)
        {
            using var linePoints = new NativeArray<Vector3>(points, Allocator.TempJob).Reinterpret<float3>();
            var scaleAndOffsetPoint = new ScaleAndOffsetPoint(offset, 1f);
            Draw(linePoints, in scaleAndOffsetPoint, color, thickness);
            linePoints.Dispose();
        }
        #endregion Drawing Tasks

        #region Data To Mesh
        /// <summary>
        /// 
        /// </summary>
        /// <param name="meshData"></param>
        /// <param name="drawObject"></param>
        public static void DrawMesh(in MeshData meshData, in ObjectForMeshDrawing drawObject)
        {
            drawObject.Filter.mesh.Clear();
            meshData.VerticesAndNormalsHandle.Complete();
            drawObject.Filter.mesh.SetVertices(meshData.Vertices);
            drawObject.Filter.mesh.SetNormals(meshData.Normals);

            meshData.TrianglesHandle.Complete();
            drawObject.Filter.mesh.SetTriangles(meshData.Triangles.ToArray(), 0);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="converter"></param>
        /// <param name="thickness"></param>
        /// <param name="dependency"></param>
        /// <typeparam name="TData"></typeparam>
        /// <typeparam name="TConverter"></typeparam>
        /// <returns></returns>
        public static MeshData ConstructMesh<TData, TConverter>(NativeArray<TData> data, 
                                                                in TConverter converter, 
                                                                float thickness = 0.01f, 
                                                                JobHandle dependency = default)
            where TData : struct
            where TConverter : IConvertToWorldPoint<TData>
        {
            int dataLength = data.Length;
            int numberOfTriangles = 2 * (4 * dataLength - 4);
            NativeArray<int> triangles = new NativeArray<int>(3 * numberOfTriangles, Allocator.TempJob);
            JobHandle trianglesHandle = new BuildTrianglesFromCuboidVertices
            {
                Triangles = triangles,
                VertexLength = 4 * dataLength
            }.Schedule(4 * dataLength - 4, BatchCount);

            NativeArray<float3> lineVertices = new NativeArray<float3>(dataLength, Allocator.TempJob);
            JobHandle vertexJob = new ConvertToVertexPoint<TData, TConverter>
            {
                Data = data,
                Converter = converter,
                Vertices = lineVertices
            }.Schedule(dataLength, BatchCount, dependency);
            
            NativeArray<float3> vertices = new NativeArray<float3>(4 * dataLength, Allocator.TempJob);
            NativeArray<float3> normals = new NativeArray<float3>(4 * dataLength, Allocator.TempJob);
            JobHandle verticesAndNormalsHandle = new ExtrudeLineVerticesToSquareFaces
            {
                CuboidVertices = vertices,
                Normals = normals,
                LineVertices = lineVertices,
                Thickness = thickness
            }.Schedule(data.Length, BatchCount, vertexJob);
            lineVertices.Dispose(verticesAndNormalsHandle);

            return new MeshData(vertices, normals, triangles, verticesAndNormalsHandle, trianglesHandle);
        }
        #endregion Data To Mesh

        #region Private
        private static void Draw<TData, TConverter>(NativeArray<TData> data,
                                                    in TConverter converter,
                                                    Color color,
                                                    float thickness,
                                                    JobHandle dependency = default) 
            where TData : struct
            where TConverter : IConvertToWorldPoint<TData>
        {
            using MeshData meshData = ConstructMesh(data, in converter, thickness, dependency);
            DrawMesh(meshData, GetObjectForDrawing(color));
        }
        
        private static void DrawAndGetObject<TData, TConverter>(NativeArray<TData> data, 
                                                                in TConverter converter, 
                                                                Color color, 
                                                                float thickness, 
                                                                JobHandle dependency,
                                                                out ObjectForMeshDrawing drawingObject) 
            where TData : struct
            where TConverter : IConvertToWorldPoint<TData>
        {
            using MeshData meshData = ConstructMesh(data, in converter, thickness, dependency);
            drawingObject = GetObjectForDrawing(color);
            DrawMesh(meshData, in drawingObject);
        }

        private static ObjectForMeshDrawing GetObjectForDrawing(Color color = default)
        {
            if (GameObjectPool.Count > 0)
            {
                GameObject pooledObject = GameObjectPool.Dequeue();
                return new ObjectForMeshDrawing(pooledObject, LineMaterial, color);
            }

            return new ObjectForMeshDrawing(new GameObject("DrawObject"), LineMaterial, color);
        }
        #endregion Private
    }
}