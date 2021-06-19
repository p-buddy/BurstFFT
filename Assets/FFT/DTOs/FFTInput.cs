using Unity.Collections;
using Unity.Jobs;

namespace FFT
{
    public readonly struct FFTInput<T> where T : struct
    {
        public FFTSize Size { get; }
        public NativeArray<T> Data { get; }
        
        public JobHandle? Handle { get; }

        public FFTInput(NativeArray<T> data)
        {
            Size = new FFTSize(data.Length);
            Data = data;
            Handle = null;
        }
        
        public FFTInput(NativeArray<T> data, in FFTSize size)
        {
            Size = size;
            Data = data;
            Handle = null;
        }
        
        public FFTInput(in FFTInput<T> input, JobHandle handle)
        {
            Size = input.Size;
            Data = input.Data;
            Handle = handle;
        }
    }
}