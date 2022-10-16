using Unity.Entities;
using UnityEngine;

namespace JamUp.Waves.RuntimeScripts
{
    public struct VertexCount: IComponentData, IValueSettable<int>, IRequiredInArchetype
    {
        [field: SerializeField]
        public int Value { get; set; }
    }
}