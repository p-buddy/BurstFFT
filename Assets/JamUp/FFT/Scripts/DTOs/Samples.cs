using System;
using JamUp.Math;
using Unity.Collections;
using Unity.Jobs;

namespace JamUp.FFT
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
        public int SourceDataLength { get; }

        public Samples(NativeArray<T> data, Allocator allocatorIfResizeRequired)
        {
            SourceDataLength = data.Length;
            
            if (PowersOf2.IsPowerOf2(SourceDataLength))
            {
                this.data = data;
                Handle = default;
                return;
            }

            int length = PowersOf2.Next(SourceDataLength);
            this.data = new NativeArray<T>(length, allocatorIfResizeRequired);
            
            Handle = new CopyJob<T>
            {
                From = data,
                To = this.data,
            }.Schedule(SourceDataLength, 64);
        }
        
        public Samples(NativeArray<T> data, JobHandle handle, Allocator allocatorIfResizeRequired)
        {
            SourceDataLength = data.Length;

            if (PowersOf2.IsPowerOf2(SourceDataLength))
            {
                this.data = data;
                Handle = handle;
                return;
            }
            
            int length = PowersOf2.Next(SourceDataLength);
            this.data = new NativeArray<T>(length, allocatorIfResizeRequired);
            
            Handle = new CopyJob<T>
            {
                From = data,
                To = this.data,
            }.Schedule(SourceDataLength, 64, handle);
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