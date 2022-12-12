using System;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.Waves.RuntimeScripts
{
    public static class AnimationLerp
    {
        public static float LerpBetween(this float2 bounds, float t) => math.lerp(bounds.x, bounds.y, t);
        public static float Lerp(this Animation<float> animation, float t)
        {
            float s = animation.Curve.GetLerpParameter(t);
            return math.lerp(animation.From, animation.To, s);
        }
        
        [BurstCompile]
        public static float GetLerpParameter(this AnimationCurve curve, float t)
        {
            switch (curve)
            {
                case AnimationCurve.Linear:
                    return t;
                case AnimationCurve.Instant:
                    return 1;
                case AnimationCurve.EaseBeginSine:
                    break;
                case AnimationCurve.EaseEndSine:
                    break;
                case AnimationCurve.EaseBeginEndSine:
                    break;
                case AnimationCurve.EaseBeginQuad:
                    break;
                case AnimationCurve.EaseEndQuad:
                    break;
                case AnimationCurve.EaseBeginEndQuad:
                    break;
                case AnimationCurve.EaseBeginCubic:
                    break;
                case AnimationCurve.EaseEndCubic:
                    break;
                case AnimationCurve.EaseBeginEndCubic:
                    break;
                case AnimationCurve.EaseBeginQuart:
                    break;
                case AnimationCurve.EaseEndQuart:
                    break;
                case AnimationCurve.EaseBeginEndQuart:
                    break;
                case AnimationCurve.EaseBeginQuint:
                    break;
                case AnimationCurve.EaseEndQuint:
                    break;
                case AnimationCurve.EaseBeginEndQuint:
                    break;
                case AnimationCurve.EaseBeginExpo:
                    break;
                case AnimationCurve.EaseEndExpo:
                    break;
                case AnimationCurve.EaseBeginEndExpo:
                    break;
                case AnimationCurve.EaseBeginCirc:
                    break;
                case AnimationCurve.EaseEndCirc:
                    break;
                case AnimationCurve.EaseBeginEndCirc:
                    break;
                case AnimationCurve.EaseBeginBack:
                    break;
                case AnimationCurve.EaseEndBack:
                    break;
                case AnimationCurve.EaseBeginEndBack:
                    break;
                case AnimationCurve.EaseBeginElastic:
                    break;
                case AnimationCurve.EaseEndElastic:
                    break;
                case AnimationCurve.EaseBeginEndElastic:
                    break;
                case AnimationCurve.EaseBeginBounce:
                    break;
                case AnimationCurve.EaseEndBounce:
                    break;
                case AnimationCurve.EaseBeginEndBounce:
                    break;
            }
            throw new ArgumentOutOfRangeException(nameof(curve), curve, null);
        }
    }
}