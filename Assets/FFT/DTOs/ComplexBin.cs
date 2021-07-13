using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace FFT
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ComplexBin
    {
        public float2 Vector { get; }
        public float Real => Vector.x;
        public float Imaginary => Vector.y;
        public float Magnitude => math.length(Vector);
        public float SignedMagnitude => math.sign(Real) * Magnitude;
        public float Phase => math.atan2(Imaginary, Real);
        public ComplexBin Conjugate => new ComplexBin(Real, -Imaginary);

        public ComplexBin(float2 value)
        {
            Vector = value;
        }
        
        public ComplexBin(float value)
        {
            Vector = new float2(value, 0f);
        }
        
        public ComplexBin(float real, float im)
        {
            Vector = new float2(real, im);
        }
        
        public override string ToString()
        {
            return $"({Real} + {Imaginary}i)";
        }
        
        public static ComplexBin FromPolar(float r, float radians)
        {
            return new ComplexBin(r * math.cos(radians), r * math.sin(radians));
        }

        public static ComplexBin operator +(ComplexBin a, ComplexBin b)
        {
            return new ComplexBin(a.Real + b.Real, a.Imaginary + b.Imaginary);
        }
        
        public static ComplexBin operator -(ComplexBin a, ComplexBin b)
        {
            ComplexBin data = new ComplexBin(a.Real - b.Real, a.Imaginary - b.Imaginary);
            return data;
        }
        
        public static ComplexBin operator *(ComplexBin a, ComplexBin b)
        {
            return new ComplexBin((a.Real * b.Real) - (a.Imaginary * b.Imaginary),
                (a.Real * b.Imaginary + a.Imaginary * b.Real));
        }
        
        public static ComplexBin operator *(ComplexBin a, float b)
        {
            return new ComplexBin(a.Real * b, a.Imaginary * b);
        }

        public const int SizeOf = sizeof(float) * 2;
    }
}