using Unity.Entities;
using UnityEngine;

namespace JamUp.Waves.RuntimeScripts
{
    public struct CurrentThickness: IComponentData, IValueSettable<Animation<float>>, IRequiredInArchetype
    {
        [field: SerializeField]
        public Animation<float> Value { get; set; }
        public static implicit operator Animation<float>(CurrentThickness projection) => projection.Value;
    }
}