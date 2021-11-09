using System;

namespace JamUp.NativeArrayUtility
{
    public readonly struct SortedWithIndex<T> : IComparable<SortedWithIndex<T>> where T : unmanaged, IComparable<T>
    {
        public int Index { get; }
        public T Value { get; }

        public SortedWithIndex(T value, int index)
        {
            Value = value;
            Index = index;
        }

        public int CompareTo(SortedWithIndex<T> other)
        {
            return Value.CompareTo(other.Value);
        }
    }
}