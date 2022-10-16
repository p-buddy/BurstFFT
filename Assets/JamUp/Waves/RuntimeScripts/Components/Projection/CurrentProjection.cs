using Unity.Entities;
using UnityEngine;

namespace JamUp.Waves.RuntimeScripts
{
    public struct CurrentProjection: IComponentData, IValueSettable<Animation<float>>, IRequiredInArchetype
    {
        [field: SerializeField]
        public Animation<float> Value { get; set; }
        public static implicit operator Animation<float>(CurrentProjection projection) => projection.Value;
    }
}