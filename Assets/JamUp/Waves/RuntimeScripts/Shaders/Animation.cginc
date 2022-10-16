#include "Math.cginc"

static const int Linear = 0;
static const int Instant = 1;
static const int EaseBeginSine = 2 * Instant;
static const int EaseEndSine = 2 * EaseBeginSine;
static const int EaseBeginEndSine = 2 * EaseEndSine;
static const int EaseBeginQuad = 2 * EaseBeginEndSine; 
static const int EaseEndQuad = 2 * EaseBeginQuad; 
static const int EaseBeginEndQuad = 2 * EaseEndQuad;
static const int EaseBeginCubic = 2 * EaseBeginEndQuad;
static const int EaseEndCubic = 2 * EaseBeginCubic;
static const int EaseBeginEndCubic = 2 * EaseEndCubic;
static const int EaseBeginQuart = 2 * EaseBeginEndCubic; 
static const int EaseEndQuart = 2 * EaseBeginQuart;
static const int EaseBeginEndQuart = 2 * EaseEndQuart;
static const int EaseBeginQuint = 2 * EaseBeginEndQuart;
static const int EaseEndQuint = 2 * EaseBeginQuint;
static const int EaseBeginEndQuint = 2 * EaseEndQuint; 
static const int EaseBeginExpo = 2 * EaseBeginEndQuint; 
static const int EaseEndExpo = 2 * EaseBeginExpo;
static const int EaseBeginEndExpo = 2 * EaseEndExpo;
static const int EaseBeginCirc = 2 * EaseBeginEndExpo;
static const int EaseEndCirc = 2 * EaseBeginCirc;
static const int EaseBeginEndCirc = 2 * EaseEndCirc;
static const int EaseBeginBack = 2 * EaseBeginEndCirc;
static const int EaseEndBack = 2 * EaseBeginBack;
static const int EaseBeginEndBack = 2 * EaseEndBack;
static const int EaseBeginElastic = 2 * EaseBeginEndBack;
static const int EaseEndElastic = 2 * EaseBeginElastic;
static const int EaseBeginEndElastic = 2 * EaseEndElastic;
static const int EaseBeginBounce = 2 * EaseBeginEndElastic;
static const int EaseEndBounce = 2 * EaseBeginBounce;
static const int EaseBeginEndBounce = 2 * EaseEndBounce;

float ApplyAnimationCurve(const float x, const int curve)
{
    float total = max(1 - curve, 0) * x;
    
    int powerOf2 = 1;

    // Instant
    total += float((Instant & curve) >> powerOf2) * x;
    powerOf2 *= 2;
    
    // Ease in sine
    total += float((EaseBeginSine & curve) >> powerOf2) * (1 - cos(x * PI / 2));
    powerOf2 *= 2;

    // Ease out sine

    total += float((EaseEndSine & curve) >> powerOf2 ) * sin(x * PI / 2);
    powerOf2 *= 2;

    // Ease in out sine

    total += float((EaseBeginEndSine & curve) >> powerOf2 ) * -(cos(PI * x) - 1) / 2;;
    powerOf2 *= 2;

    // total += float(( & curve) >> powerOf2 ) * ;
    // powerOf2 *= 2;
    
    return total;
}