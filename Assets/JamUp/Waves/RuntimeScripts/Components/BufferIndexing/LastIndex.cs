using Unity.Entities;

namespace JamUp.Waves.RuntimeScripts.BufferIndexing
{
    public readonly struct LastIndex: IComponentData, IRequiredInArchetype
    {
        public int Value { get; }

        public LastIndex(int index)
        {
            Value = index;
        }
    }
}