using JamUp.UnityUtility;
using Unity.Collections;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public struct SetCurrentSetting<T>: IJob where T: struct
    {
        [ReadOnly]
        public int Index;
        
        [ReadOnly]
        public NativeArray<T> AllSettings;
        
        [WriteOnly]
        public NativeArray<ShaderProperty<T>> Output;

        public void Execute()
        {
            Output[0] = Output[0].WithValue(AllSettings[Index]);
        }
    }
}