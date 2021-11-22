using System.IO;
using System.Runtime.InteropServices;
using JamUp.StringUtility;
using Unity.Mathematics;

namespace JamUp.Waves
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CoordinateAxes
    {
        public float3 Right;
        public float3 Up;
        public float3 Forward;

        public override string ToString()
        {
            return this.NameAndPublicData(true);
        }
    }
}