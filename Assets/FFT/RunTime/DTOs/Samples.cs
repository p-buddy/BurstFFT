using System;
using Unity.Collections;
using Unity.Jobs;

namespace FFT
{
    public readonly struct Samples<T> : IDisposable where T : struct
    {
        private readonly NativeArray<T> data;
        public JobHandle Handle { get; }

        public NativeArray<T> Data
        {
            get
            {
                Handle.Complete();
                return data; 
            }
        }

        public NativeArray<T> DeferredData => data;
        public int Length => data.Length;

        public Samples(NativeArray<T> data)
        {
            this.data = data;
            Handle = default;
        }
        
        public Samples(NativeArray<T> data, JobHandle handle)
        {
            this.data = data;
            Handle = handle;
        }

        public void Dispose()
        {
            data.Dispose();
        }
        
        public void Dispose(JobHandle handle)
        {
            data.Dispose(handle);
        }
    }
}