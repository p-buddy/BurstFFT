using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace JamUp.NativeArrayUtility
{
    public struct CompareNativeArraysJob<TData, TNativeArrayIdentifier, TComparisonEvent> : IJobParallelFor
        where TData : struct, IEquatable<TData>
        where TComparisonEvent : struct, INativeArrayComparisonEvent<TComparisonEvent, TNativeArrayIdentifier, int>
    {
        [ReadOnly]
        public TNativeArrayIdentifier IdentifierA;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<TData> A;

        [ReadOnly]
        public TNativeArrayIdentifier IdentifierB;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<TData> B;

        [ReadOnly]
        public ComparisonEvent Expected;
            
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeList<TComparisonEvent>.ParallelWriter Failures;
            
        public void Execute(int index)
        {
            bool success = default;
            switch (Expected)
            {
               case ComparisonEvent.Equal:
                   success = A[index].Equals(B[index]);
                   break;
               case ComparisonEvent.NotEqual:
                   success = !A[index].Equals(B[index]);
                   break;
            }

            if (success)
            {
                return;
            }
            
            Failures.AddNoResize(default(TComparisonEvent).CreateComparisonEvent(IdentifierA, IdentifierB, index));
        }
    }
}