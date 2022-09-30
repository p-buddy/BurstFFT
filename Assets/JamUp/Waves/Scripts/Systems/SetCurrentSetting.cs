using Unity.Collections;
using Unity.Jobs;

namespace JamUp.Waves.Scripts.Systems
{
    public struct SetCurrentSetting<T>: IJob where T: struct
    {
        [ReadOnly]
        public int CurrentIndex;
        
        [ReadOnly]
        public NativeArray<T> AllSettings;
        
        [WriteOnly]
        public NativeArray<T> Output;

        public void Execute()
        {
            Output[0] = AllSettings[CurrentIndex];
        }
    }
}