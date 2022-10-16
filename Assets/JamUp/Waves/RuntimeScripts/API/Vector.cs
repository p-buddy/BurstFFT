using Unity.Mathematics;
using UnityEngine;

namespace JamUp.Waves.RuntimeScripts.API
{
    public readonly struct Vector
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public Vector(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        public static implicit operator float3(Vector f) => new float3(f.X, f.Y, f.Z);
        public static implicit operator Vector3(Vector f) => new Vector3(f.X, f.Y, f.Z);
    }
}