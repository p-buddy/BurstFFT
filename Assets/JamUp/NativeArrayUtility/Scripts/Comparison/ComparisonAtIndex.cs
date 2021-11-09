using System;
using Unity.Collections;

namespace JamUp.NativeArrayUtility
{
    public readonly struct ComparisonAtIndex<TIdentifier> : IComparable<ComparisonAtIndex<TIdentifier>>,
        INativeArrayComparisonEvent<ComparisonAtIndex<TIdentifier>, TIdentifier, int> where TIdentifier : struct
    {
        public TIdentifier IdentifierA { get; }
        public TIdentifier IdentifierB { get; }
        public int AtIndex { get; }

        public ComparisonAtIndex(TIdentifier identifierA, TIdentifier identifierB, int atIndex)
        {
            IdentifierA = identifierA;
            IdentifierB = identifierB;
            AtIndex = atIndex;
        }

        public ComparisonAtIndex<TIdentifier> CreateComparisonEvent(TIdentifier identifierA, TIdentifier identifierB, int info)
        {
            return new ComparisonAtIndex<TIdentifier>(identifierA, identifierB, info);
        }

        public int CompareTo(ComparisonAtIndex<TIdentifier> other)
        {
            return AtIndex.CompareTo(other.AtIndex);
        }
    }
}