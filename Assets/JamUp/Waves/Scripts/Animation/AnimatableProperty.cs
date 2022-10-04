using pbuddy.TypeScriptingUtility.RuntimeScripts;

namespace JamUp.Waves.Scripts
{
    public readonly struct AnimatableProperty<T> : IAnimatable, IValuable<T> where T: new()
    {
        public T Value { get; }
        public AnimationCurve AnimationCurve { get; }

        public AnimatableProperty(T value, AnimationCurve animation = AnimationCurve.Linear)
        {
            Value = value;
            AnimationCurve = animation;
        }

        public static implicit operator T(AnimatableProperty<T> prop) => prop.Value;
    }
}