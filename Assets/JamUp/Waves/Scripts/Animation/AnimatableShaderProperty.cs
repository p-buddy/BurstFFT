using JamUp.UnityUtility;

namespace JamUp.Waves.Scripts
{
    public readonly struct AnimatableShaderProperty<T>
    {
        public ShaderProperty<AnimationCurve> Animation { get; }
        public ShaderProperty<T> From { get; }
        public ShaderProperty<T> To { get; }


        public AnimatableShaderProperty(string name)
        {
            Animation = new ($"{name}Animation");
            From = new ($"{name}From");
            To = new ($"{name}To");
        }
        
        private AnimatableShaderProperty(ShaderProperty<T> from, ShaderProperty<T> to, ShaderProperty<AnimationCurve> animation)
        {
            Animation = animation;
            From = from;
            To = to;
        }

        public AnimatableShaderProperty<T> WithValues(T from, T to, AnimationCurve animation) =>
            new(From.WithValue(from), To.WithValue(to), Animation.WithValue(animation));
    }
}