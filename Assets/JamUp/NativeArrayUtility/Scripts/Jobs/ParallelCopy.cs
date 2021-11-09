using Unity.Collections;
using Unity.Jobs;

namespace JamUp.NativeArrayUtility
{
    public struct ParallelCopy<TFrom, TTo, TFromToConverter> : IJobParallelFor where TFrom : struct
                                                                               where TTo : struct
                                                                               where TFromToConverter : IConverter<TFrom, TTo>
    {
        [ReadOnly]
        public TFromToConverter Converter;
        
        [ReadOnly]
        public NativeArray<TFrom> From;
        
        [WriteOnly]
        public NativeArray<TTo> To;
        
        public void Execute(int index)
        {
            To[index] = Converter.Convert(From[index], index);
        }
    }
}