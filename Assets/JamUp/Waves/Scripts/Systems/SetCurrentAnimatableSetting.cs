using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Collections;
using Unity.Jobs;

namespace JamUp.Waves.Scripts.Systems
{
    public struct SetCurrentAnimatableSetting<T>: IJob where T: struct
    {
        [ReadOnly]
        public int CurrentIndex;
        
        [ReadOnly]
        public bool IsLast;
        
        [ReadOnly]
        public NativeArray<AnimatableProperty<T>> AllSettings;
        
        [WriteOnly]
        public NativeArray<AnimatableShaderProperty<T>> Output;

        public void Execute()
        {
            AnimatableProperty<T> from = AllSettings[CurrentIndex];
            AnimatableProperty<T> to = !IsLast ? AllSettings[CurrentIndex + 1] : from;
            Output[0] = Output[0].WithValues(from.Value, to.Value, from.Animation);
        }
    }
}