using System.Runtime.InteropServices;

namespace JamUp.Waves.RuntimeScripts
{
    public static class GCHandleUtility
    {
        public static GCHandle GetHandle(this object obj, GCHandleType type = GCHandleType.Normal) 
            => GCHandle.Alloc(obj, type); 
    }
}