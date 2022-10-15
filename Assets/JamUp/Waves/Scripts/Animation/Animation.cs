using UnityEngine;

namespace JamUp.Waves.Scripts
{
    public readonly struct Animation<T>
    {
        [field: SerializeField]
        public T From { get; }
        
        [field: SerializeField]
        public T To { get; }
        
        [field: SerializeField]
        public AnimationCurve Curve { get; }

        public Animation(T from, T to, AnimationCurve curve)
        {
            From = from;
            To = to;
            Curve = curve;
        }
    }
}