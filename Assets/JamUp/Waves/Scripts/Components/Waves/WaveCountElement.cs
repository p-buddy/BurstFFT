using Unity.Entities;
using UnityEngine;

namespace JamUp.Waves.Scripts
{
    public struct WaveCountElement: IBufferElementData, IValueSettable<int>, IRequiredInArchetype
    {
        [field: SerializeField]
        public int Value { get; set; }
    }
}