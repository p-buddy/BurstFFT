using System;
using Unity.Collections;
using Unity.Jobs;

namespace JamUp.NativeArrayUtility
{
    public struct SortWithIndex<T> : IJob where T : struct, IComparable<T>
    {
        public NativeArray<T> ToSort;
        
        public void Execute()
        {
            ToSort.Sort();
        }
    }
}