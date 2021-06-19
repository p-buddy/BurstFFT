using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace FFT
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct ComplexBin
    {
        public float2 Vector { get; }
        public float Real => Vector.x;
        public float Imaginary => Vector.y;

        public float Magnitude => math.length(Vector);
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
            ComplexBin data = new ComplexBin(r * math.cos(radians), r * math.sin(radians));
            return data;
        }

        public static ComplexBin operator +(ComplexBin a, ComplexBin b)
        {
            ComplexBin data = new ComplexBin(a.Real + b.Real , a.Imaginary + b.Imaginary);
            return data;
        }
        
        public static ComplexBin operator -(ComplexBin a, ComplexBin b)
        {
            ComplexBin data = new ComplexBin(a.Real - b.Real , a.Imaginary - b.Imaginary);
            return data;
        }
        
        public static ComplexBin operator *(ComplexBin a, ComplexBin b)
        {
            ComplexBin data = new ComplexBin((a.Real * b.Real) - (a.Imaginary * b.Imaginary),
                (a.Real * b.Imaginary + a.Imaginary * b.Real));
            return data;
        }
        
        public static ComplexBin operator *(ComplexBin a, float b)
        {
            ComplexBin data = new ComplexBin(a.Real * b, a.Imaginary * b);
            return data;
        }

        public const int SizeOf = sizeof(float) * 4;
    }
}