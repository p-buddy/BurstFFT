using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace JamUp.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Complex : IEquatable<Complex>
    {
        public float2 Vector { get; }
        
        public float Real => Vector.x;
        public float Imaginary => Vector.y;
        public float Magnitude => math.length(Vector);
        public float SignedMagnitude => math.sign(Real) * Magnitude;
        public float Phase => math.atan2(Imaginary, Real);
        public Complex Conjugate => new Complex(Real, -Imaginary);

        public Complex(float2 value)
        {
            Vector = value;
        }
        
        public Complex(float value)
        {
            Vector = new float2(value, 0f);
        }
        
        public Complex(float real, float im)
        {
            Vector = new float2(real, im);
        }
        
        public override string ToString()
        {
            return $"({Real} + {Imaginary}i)";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex FromPolar(float radius, float radians)
        {
            return new Complex(radius * math.cos(radians), radius * math.sin(radians));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator +(Complex a, Complex b)
        {
            return new Complex(a.Real + b.Real, a.Imaginary + b.Imaginary);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator -(Complex a, Complex b)
        {
            return new Complex(a.Real - b.Real, a.Imaginary - b.Imaginary);;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator *(Complex a, Complex b)
        {
            return new Complex((a.Real * b.Real) - (a.Imaginary * b.Imaginary),
                (a.Real * b.Imaginary + a.Imaginary * b.Real));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex operator *(Complex a, float b)
        {
            return new Complex(a.Real * b, a.Imaginary * b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Complex other)
        {
            return Vector.Equals(other.Vector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is Complex other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Vector.GetHashCode();
        }
    }
}