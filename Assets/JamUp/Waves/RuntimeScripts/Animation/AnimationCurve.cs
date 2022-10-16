namespace JamUp.Waves.RuntimeScripts
{
    public enum AnimationCurve
    {
        Linear = 0,
        Instant = 1,
        EaseBeginSine = 2 * Instant,
        EaseEndSine = 2 * EaseBeginSine,
        EaseBeginEndSine = 2 * EaseEndSine,
        EaseBeginQuad = 2 * EaseBeginEndSine, 
        EaseEndQuad = 2 * EaseBeginQuad, 
        EaseBeginEndQuad = 2 * EaseEndQuad,
        EaseBeginCubic = 2 * EaseBeginEndQuad,
        EaseEndCubic = 2 * EaseBeginCubic,
        EaseBeginEndCubic = 2 * EaseEndCubic,
        EaseBeginQuart = 2 * EaseBeginEndCubic, 
        EaseEndQuart = 2 * EaseBeginQuart,
        EaseBeginEndQuart = 2 * EaseEndQuart,
        EaseBeginQuint = 2 * EaseBeginEndQuart,
        EaseEndQuint = 2 * EaseBeginQuint,
        EaseBeginEndQuint = 2 * EaseEndQuint, 
        EaseBeginExpo = 2 * EaseBeginEndQuint, 
        EaseEndExpo = 2 * EaseBeginExpo,
        EaseBeginEndExpo = 2 * EaseEndExpo,
        EaseBeginCirc = 2 * EaseBeginEndExpo,
        EaseEndCirc = 2 * EaseBeginCirc,
        EaseBeginEndCirc = 2 * EaseEndCirc,
        EaseBeginBack = 2 * EaseBeginEndCirc,
        EaseEndBack = 2 * EaseBeginBack,
        EaseBeginEndBack = 2 * EaseEndBack,
        EaseBeginElastic = 2 * EaseBeginEndBack,
        EaseEndElastic = 2 * EaseBeginElastic,
        EaseBeginEndElastic = 2 * EaseEndElastic,
        EaseBeginBounce = 2 * EaseBeginEndElastic,
        EaseEndBounce = 2 * EaseBeginBounce,
        EaseBeginEndBounce = 2 * EaseEndBounce 
    }
}