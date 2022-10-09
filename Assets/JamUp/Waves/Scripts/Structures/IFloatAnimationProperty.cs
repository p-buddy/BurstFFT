using Unity.Entities;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public interface IFloatAnimationProperty
    {
        JobHandle UpdateCurrent(ComponentSystemBase system, JobHandle dependency);
        
        JobHandle SetBlock(ComponentSystemBase system,
                           JobHandle dependency,
                           ComponentTypeHandle<PropertyBlockReference> propertyBlockHandle);
    }
}