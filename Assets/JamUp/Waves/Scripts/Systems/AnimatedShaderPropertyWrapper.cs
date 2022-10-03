using System;
using JamUp.UnityUtility;
using Unity.Collections;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public readonly struct AnimatedShaderPropertyWrapper<T>: IDisposable, IShaderPropertyWrapper where T : struct
    {
        public NativeArray<AnimatableProperty<T>> AllSettings { get; }
        public NativeArray<AnimatableShaderProperty<T>> CurrentSetting { get; }
        public AnimatedShaderPropertyWrapper(NativeArray<AnimatableProperty<T>> allSettings, string propertyName)
        {
            AllSettings = allSettings;
            NativeArray<AnimatableShaderProperty<T>> current = new (0, Allocator.Persistent);
            current[0] = new AnimatableShaderProperty<T>(propertyName);
            CurrentSetting = current;
            Update(0, false).Complete();
        }

        public JobHandle Update(int index, bool isLast)
        {
            return new SetCurrentAnimatableSetting<T>
            {
                Index = index,
                IsLast = isLast,
                AllSettings = AllSettings,
                Output = CurrentSetting,
            }.Schedule();
        }

        public TDesired Constant<TDesired>() => throw new NotImplementedException();

        public AnimatableProperty<TDesired> Animated<TDesired>(float lerpTime, IShaderPropertyWrapper.LerpFromTo<TDesired> lerp)
            where TDesired : new()
        {
            var current = CurrentSetting[0];
            TDesired from = current.From.Value switch
            {
                TDesired converted => converted,
                _ => throw new Exception("Incorrect type")
            };

            TDesired to = current.To.Value switch
            {
                TDesired converted => converted,
                _ => throw new Exception("Incorrect type")
            };

            return new AnimatableProperty<TDesired>(lerp(from, to, lerpTime), current.Animation.Value);
        }

        // UPDATE!! Needs to handle lerping value
        public TConvert Current<TConvert>()
        {
            switch (CurrentSetting[0].From)
            {
                case TConvert converted:
                    return converted;
            }

            throw new Exception("Incorrect type!");
        }

        public void Dispose()
        {
            AllSettings.Dispose();
            CurrentSetting.Dispose();
        }
    }
}