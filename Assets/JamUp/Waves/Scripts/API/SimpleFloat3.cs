using Unity.Mathematics;
using UnityEngine;

namespace JamUp.Waves.Scripts.API
{
    public readonly struct SimpleFloat3
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        
        public static implicit operator float3(SimpleFloat3 f) => new float3(f.X, f.Y, f.Z);
        public static implicit operator Vector3(SimpleFloat3 f) => new Vector3(f.X, f.Y, f.Z);
    }
}