using System.Runtime.InteropServices;
using Unity.Mathematics;

using pbuddy.StringUtility.RuntimeScripts;

namespace JamUp.Waves
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DebugVertexPosition
    {
        public uint VertexIndexWithinSection; // 0 - 23
        public uint SequenceIndexWithinTriangle; // 0 - 2
        public uint TriangleNumber; // 0 - 7
        public uint TriangleType; // 0 or 1
        public uint AnchorIndex; // 0 - 3
        public uint AnchorPredecessorIndex; // 0 - 3
        public uint AnchorSuccessorIndex; // 0 - 3
        public float3 Up;
        public float3 Right;
        public uint FinalIndex;

        public override string ToString()
        {
            return this.NameAndPublicData(false);
        }
    };
}