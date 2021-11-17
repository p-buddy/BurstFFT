using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace JamUp.DataVisualization
{
    public struct ExtrudeLineVerticesToSquareFaces : IJobParallelFor
    {
        private enum Quadrant { I, II, III, IV }
        
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> LineVertices;
        
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> CuboidVertices;
        
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> Normals;

        [ReadOnly]
        public float Thickness;

        public void Execute(int index)
        {
            float3 lineVertex = LineVertices[index];
            float3 hereFromPrev = index > 0 ? SafeNormalize(lineVertex - LineVertices[index - 1], math.forward()) : default;
            float3 nextFromHere = index < LineVertices.Length - 1 ? SafeNormalize(LineVertices[index + 1] - lineVertex, math.forward()) : default;
            float3 forward = SafeNormalize(0.5f * (hereFromPrev + nextFromHere), math.forward());

            float halfThickness = 0.5f * Thickness;
            float3 up = forward.Equals(math.forward()) || forward.Equals(-math.forward()) ? math.sign(forward.z) * math.up() : math.normalize(math.cross(forward, math.forward()));
            float3 right = math.cross(forward, up);

            up *= halfThickness;
            right *= halfThickness;

            int newIndex = index * 4;
            SetVertexAndNormal(lineVertex, up, right, Quadrant.I, newIndex);
            SetVertexAndNormal(lineVertex, up, right, Quadrant.II, newIndex + 1);
            SetVertexAndNormal(lineVertex, up, right, Quadrant.III, newIndex + 2);
            SetVertexAndNormal(lineVertex, up, right, Quadrant.IV, newIndex + 3);
        }

        private void SetVertexAndNormal(float3 lineVertex, float3 up, float3 right, Quadrant quadrant, int indexToSet)
        {
            float3 offset = GetOffset(up, right, quadrant);
            CuboidVertices[indexToSet] = lineVertex + offset;
            Normals[indexToSet] = math.normalize(offset);
        }

        private float3 GetOffset(float3 up, float3 right, Quadrant quadrant)
        {
            switch (quadrant)
            {
                case Quadrant.I:
                    return up + right;
                case Quadrant.II:
                    return up - right;
                case Quadrant.III:
                    return -up - right;
                case Quadrant.IV:
                    return -up + right;
                default:
                    return default;
            }
        }
        
        private float3 SafeNormalize(float3 vector, float3 fallback) =>
            !vector.Equals(float3.zero) ? math.normalize(vector) : fallback;
    }
}