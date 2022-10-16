using pbuddy.TypeScriptingUtility.RuntimeScripts;

namespace JamUp.Waves.RuntimeScripts
{
    public readonly struct Animatable<T> : IAnimatable, IValuable<T> where T: new()
    {
        public T Value { get; }
        public AnimationCurve AnimationCurve { get; }

        public Animatable(T value, AnimationCurve animation = AnimationCurve.Linear)
        {
            Value = value;
            AnimationCurve = animation;
        }

        public static implicit operator T(Animatable<T> prop) => prop.Value;
    }
}