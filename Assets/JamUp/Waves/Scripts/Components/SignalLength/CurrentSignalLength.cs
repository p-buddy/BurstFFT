using Unity.Entities;
using UnityEngine;

namespace JamUp.Waves.Scripts
{
    public struct CurrentSignalLength: IComponentData, IValueSettable<Animation<float>>, IRequiredInArchetype
    {
        [field: SerializeField]
        public Animation<float> Value { get; set; }
        public static implicit operator Animation<float>(CurrentSignalLength signalLength) => signalLength.Value;
    }
}