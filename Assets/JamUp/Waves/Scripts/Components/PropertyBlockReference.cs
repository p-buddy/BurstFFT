using System.Runtime.InteropServices;
using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public readonly struct PropertyBlockReference: IComponentData
    {
        public int ID { get; }
        public GCHandle Handle { get;}

        public PropertyBlockReference(int id, GCHandle handle)
        {
            ID = id;
            Handle = handle;
        }
        
    }
}