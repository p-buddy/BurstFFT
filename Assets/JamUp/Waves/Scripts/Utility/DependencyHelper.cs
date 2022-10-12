using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public static class DependencyHelper
    {
        public static JobHandle CompleteAndGetBack(this JobHandle handle)
        {
            handle.Complete();
            return handle;
        }
    }
}