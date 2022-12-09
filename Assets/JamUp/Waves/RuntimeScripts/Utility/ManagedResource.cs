using System.Runtime.InteropServices;

namespace JamUp.Waves.RuntimeScripts
{
    public readonly struct ManagedResource<T>
    {
        public T Object => (T)handle.Target;
        
        private readonly GCHandle handle;

        public ManagedResource(object obj)
        {
            handle = obj.GetHandle();
        }

        public static ManagedResource<T> Hold(T obj) => new(obj);
    }
}