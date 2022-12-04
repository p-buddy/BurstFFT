using Unity.Entities;

namespace JamUp.Waves.RuntimeScripts.BufferIndexing
{
    public struct CurrentIndex: IComponentData, IRequiredInArchetype
    {
        public int Value;
        public bool IsLast(in LastIndex last) => last.Value == Value;
        public void Increment() => Value++;
        public static CurrentIndex Invalid() => new () { Value = -1 };

    }
}