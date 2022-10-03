using System;
using JamUp.UnityUtility;
using Unity.Collections;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public readonly struct ConstantShaderPropertyWrapper<T> : IDisposable, IShaderPropertyWrapper where T : struct
    {
        public NativeArray<T> AllSettings { get; }
        public NativeArray<ShaderProperty<T>> CurrentSetting { get; }
        public int Offset { get; }

        public ConstantShaderPropertyWrapper(NativeArray<T> allSettings, string propertyName, int offset = default)
        {
            AllSettings = allSettings;
            NativeArray<ShaderProperty<T>> current = new (0, Allocator.Persistent);
            current[0] = new ShaderProperty<T>(propertyName);
            CurrentSetting = current;
            Offset = offset;
            Update(0);
        }

        public JobHandle Update(int index, bool _ = default)
        {
            return new SetCurrentSetting<T>
            {
                Index = index + Offset,
                AllSettings = AllSettings,
                Output = CurrentSetting,
            }.Schedule();
        }

        public TDesired Constant<TDesired>() => CurrentSetting[0].Value switch
        {
            TDesired converted => converted,
            _ => throw new Exception("Incorrect type!")
        };

        public AnimatableProperty<TInvalid> Animated<TInvalid>(float _, IShaderPropertyWrapper.LerpFromTo<TInvalid> __) where TInvalid : new() => throw new NotImplementedException();

        public void Dispose()
        {
            AllSettings.Dispose();
            CurrentSetting.Dispose();
        }
    }

}