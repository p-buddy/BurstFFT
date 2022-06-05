using Unity.Mathematics;
using UnityEngine;

namespace JamUp.Waves
{
    public struct SimpleFloat3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        
        public static implicit operator float3(SimpleFloat3 f) => new float3(f.X, f.Y, f.Z);
        public static implicit operator Vector3(SimpleFloat3 f) => new Vector3(f.X, f.Y, f.Z);
    }
}