namespace JamUp.Waves.Scripts
{
    public readonly struct Animation<T>
    {
        public T From { get; }
        public T To { get; }
        public AnimationCurve Curve { get; }

        public Animation(T from, T to, AnimationCurve curve)
        {
            From = from;
            To = to;
            Curve = curve;
        }
    }
}