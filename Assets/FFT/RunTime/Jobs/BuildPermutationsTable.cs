using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace FFT
{ 
    internal struct BuildPermutationsTableJob : IJob
    {
        [WriteOnly]
        internal NativeArray<int2> PermutationsTable;

        [ReadOnly] 
        internal FFTSize Size;

        int Permutate(int x)
        {
            int log = Size.Log;
            return Enumerable.Range(0, log)
                      .Aggregate(0, (acc, i) => acc += ((x >> i) & 1) << (log - 1 - i));
        }
        
        public void Execute()
        {
            for (var i = 0; i < Size.Width; i += 2)
            {
                PermutationsTable[i / 2] = math.int2(Permutate(i), Permutate(i + 1));
            }
        }
    }
}