using Unity.Entities;

namespace JamUp.Waves.RuntimeScripts
{
    public struct CurrentWaveIndex: IComponentData, IRequiredInArchetype
    {
        public int Value;
        public void IncrementBy(int previousWaveCount) => Value += previousWaveCount;
    }
}