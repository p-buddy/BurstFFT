using System;

using Unity.Collections;
using Unity.Jobs;

namespace JamUp.NativeArrayUtility
{
    public static class SortUtility
    {
        private const int BatchCount = 64;

        internal struct ConvertToSortedWithIndex<T> : IConverter<T, SortedWithIndex<T>> where T : unmanaged, IComparable<T>
        {
            public SortedWithIndex<T> Convert(T value, int index)
            {
                return new SortedWithIndex<T>(value, index);
            }
        }

        public static NativeArray<SortedWithIndex<TData>> GetSortedIndices<TData>(NativeArray<TData> toSort,
            Allocator allocator,
            out JobHandle handle,
            JobHandle dependency = default) where TData : unmanaged, IComparable<TData>
        {
            NativeArray<SortedWithIndex<TData>> sortedWithIndices = new NativeArray<SortedWithIndex<TData>>(toSort.Length, allocator);
            JobHandle parallelCopy = new ParallelCopy<TData, SortedWithIndex<TData>, ConvertToSortedWithIndex<TData>>()
            {
                Converter = default,
                From = toSort,
                To = sortedWithIndices
            }.Schedule(toSort.Length, BatchCount, dependency);
            
            handle = sortedWithIndices.Sort(parallelCopy);
            return sortedWithIndices;
        }
        
        public static NativeArray<SortedWithIndex<TData>> GetSortedIndices<TData>(TData[] toSort,
            Allocator allocator,
            out JobHandle handle,
            JobHandle dependency = default) where TData : unmanaged, IComparable<TData>
        {
            NativeArray<TData> toSortNative = new NativeArray<TData>(toSort, Allocator.TempJob);
            NativeArray<SortedWithIndex<TData>> sortedWithIndices = new NativeArray<SortedWithIndex<TData>>(toSort.Length, allocator);
            JobHandle parallelCopy = new ParallelCopy<TData, SortedWithIndex<TData>, ConvertToSortedWithIndex<TData>>()
            {
                Converter = default,
                From = toSortNative,
                To = sortedWithIndices
            }.Schedule(toSort.Length, BatchCount);
            toSortNative.Dispose(parallelCopy);
            
            handle = sortedWithIndices.Sort(parallelCopy);
            return sortedWithIndices;
        }
    }
}