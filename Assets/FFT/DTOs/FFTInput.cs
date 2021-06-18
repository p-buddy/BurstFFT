using Unity.Collections;

namespace FFT
{
    public readonly struct FFTInput<T> where T : struct
    {
        public FFTSize Size { get; }
        public NativeArray<T> Data { get; }

        public FFTInput(NativeArray<T> data)
        {
            Size = new FFTSize(data.Length);
            Data = data;
        }
        
        public FFTInput(NativeArray<T> data, in FFTSize size)
        {
            Size = size;
            Data = data;
        }
    }
}