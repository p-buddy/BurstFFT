using System.Runtime.InteropServices;
using Unity.Entities;
using UnityEngine;

namespace JamUp.Waves.RuntimeScripts
{
    public readonly struct PropertyBlockReference: IComponentData, IRequiredInArchetype
    {
        [field: SerializeField]
        public int ID { get; }
        
        [field: SerializeField]
        public GCHandle Handle { get;}

        public PropertyBlockReference(int id, GCHandle handle)
        {
            ID = id;
            Handle = handle;
        }
    }
}