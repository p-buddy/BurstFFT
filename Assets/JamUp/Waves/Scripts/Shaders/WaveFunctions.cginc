#include "Wave.cginc"
#include "WaveType.cginc"
#include "Math.cginc"

float GetValueAtTime(const in Wave wave, const in float time)
{
    const float rotationAmount = 2.0f * PI * time * wave.Frequency + wave.PhaseRadians;
    const float sineValue = sin(rotationAmount);

    const float sineFactor = float(SineWaveType & wave.WaveType) * wave.Amplitude * sineValue;
    const float squareFactor = float((SquareWaveType & wave.WaveType) >> 1) * wave.Amplitude * sign(sineValue);
    const float triangleFactor = float((TriangleWaveType & wave.WaveType) >> 2) * wave.Amplitude * TwoOverPI * asin(sineValue);
    const float sawToothFactor = float((SawtoothWaveType & wave.WaveType) >> 3) * wave.Amplitude * -TwoOverPI * atan(1.0f / tan(rotationAmount - PIOverTwo));
    
    return sineFactor + squareFactor + triangleFactor + sawToothFactor;
}

float3 GetValueAtTime(const in Wave wave, const in float time, const in float3 displacementVector)
{
    return GetValueAtTime(wave, time) * displacementVector;
}

float GetTimeResolution(const in uint sampleRate)
{
    return float(1 / sampleRate);
}

float3 GetTangent(const in Wave wave, const in float time, const uint sampleRate)
{
    const float timeResolution = GetTimeResolution(sampleRate);
    const float ahead = GetValueAtTime(wave, time + timeResolution);
    const float behind = GetValueAtTime(wave, time - timeResolution);
    const float slope = ahead - behind;
    const float2 vector2D = normalize(float2(1, slope)); 
    return float3(0, vector2D.y, vector2D.x); // Imagine looking at wave sidewise, so right (x) becomes forward (z)
}

float3 GetTangent(const in Wave wave, const in float time, const in float timeResolution, const float3 displacementVector, const float3 forward)
{
    const float ahead = GetValueAtTime(wave, time + timeResolution);
    const float behind = GetValueAtTime(wave, time - timeResolution);
    const float slope = ahead - behind;
    return normalize(slope * displacementVector + forward);
}

struct CoordinateAxes
{
    float3 Right;
    float3 Up;
    float3 Forward;
};

CoordinateAxes GetCoordinateAxesAtTime(const in Wave wave, const in float time, const uint sampleRate)
{
    const float3 forward = GetTangent(wave, time, sampleRate);
    const float3 up = cross(forward, float3(0,0,1));
    const float3 right = cross(forward, up);
    CoordinateAxes frame;
    frame.Forward = forward;
    frame.Up = up;
    frame.Right = right;
    return frame;
}

CoordinateAxes GetCoordinateAxes(const in float3 forward)
{
    const float3 up = cross(forward, float3(0,0,1));
    const float3 right = cross(forward, up);
    CoordinateAxes frame;
    frame.Forward = forward;
    frame.Up = up;
    frame.Right = right;
    return frame;
}

float GetTimeForVertexIndex(const in uint vertexIndex, const in uint sampleRate)
{
    const uint sampleIndex = vertexIndex / 24;
    return float(sampleIndex) / float(sampleRate);
}

Wave SinToCos(const in Wave wave)
{
    Wave cosWave;
    cosWave.Amplitude = wave.Amplitude;
    cosWave.Frequency = wave.Frequency;
    cosWave.PhaseRadians = wave.PhaseRadians + PIOverTwo;
    cosWave.WaveType = wave.WaveType;
    return cosWave;
}

float3 GetVertexPosition(const in uint vertexIndex, const in float3 samplePosition, const in float3 nextSamplePosition, const in float3 sampleTangent, const in float3 nextSampleTangent, const in float thickness)
{
    // TRIANGLE TYPE 0: [0] (front_face) face vertex # --> [1] (front_face) face vertex # + 1 --> [2] (back_face) face vertex #
    // TRIANGLE TYPE 1: [0] (front_face) face vertex # --> [1] (back_face) face vertex # --> [2] (front_face) face vertex # - 1
    // NOTE: sequence element [0] will be referred to as the "Anchor Index" for a given triangle
    
    // (TYPE 0) triangle 0: (front face) vertex 0 --> (front face) vertex 1 --> (back face) vertex 0
    // (TYPE 0) triangle 1: (front face) vertex 1 --> (front face) vertex 2 --> (back face) vertex 1
    // (TYPE 0) triangle 2: (front face) vertex 2 --> (front face) vertex 3 --> (back face) vertex 2
    // (TYPE 0) triangle 3: (front face) vertex 3 --> (front face) vertex 0 --> (back face) vertex 3

    // (TYPE 1) triangle 4: (front face) vertex 0 --> (back face) vertex 0 --> (back_face) vertex 3
    // (TYPE 1) triangle 5: (front face) vertex 1 --> (back face) vertex 1 --> (back_face) vertex 0 
    // (TYPE 1) triangle 6: (front face) vertex 2 --> (back face) vertex 2 --> (back_face) vertex 1 
    // (TYPE 1) triangle 7: (front face) vertex 3 --> (back face) vertex 3 --> (back_face) vertex 2
    
    static const int UpFactors[4] = {1, 1, -1, -1};
    static const int RightFactors[4] = {1, -1, -1, 1};
    
    const CoordinateAxes frontAxes = GetCoordinateAxes(sampleTangent);
    const CoordinateAxes backAxes = GetCoordinateAxes(nextSampleTangent);

    const float halfThickness = 0.5f * thickness;

    const uint vertexIndexWithinSection = vertexIndex % 24; // 0 - 23
    const uint sequenceIndexWithinTriangle = vertexIndexWithinSection % 3; // 0 - 2
    const uint triangleNumber = vertexIndexWithinSection / 3; // 0 - 7
    const uint triangleType = triangleNumber / 4; // 0 or 1
    const uint anchorIndex = triangleNumber % 4; // 0 - 3
    const uint anchorPredecessorIndex = int(anchorIndex - 1) % 4; // 0 - 3
    const uint anchorSuccessorIndex = (anchorIndex + 1) % 4; // 0 - 3
    
    const bool useFrontAxes = sequenceIndexWithinTriangle == 0 || sequenceIndexWithinTriangle == 1 && triangleType == 0;
    const float3 up = float(useFrontAxes) * frontAxes.Up + float(!useFrontAxes) * backAxes.Up;
    const float3 right = float(useFrontAxes) * frontAxes.Right + float(!useFrontAxes) * backAxes.Right;
    const float3 position = float(useFrontAxes) * samplePosition + float(!useFrontAxes) * nextSamplePosition;

    const bool noOffsetFromAnchorIndex = sequenceIndexWithinTriangle == 0 || triangleType == 0 && sequenceIndexWithinTriangle == 2 || triangleType == 1 && sequenceIndexWithinTriangle == 1;
    const uint finalIndex = int(noOffsetFromAnchorIndex) * anchorIndex + int(!noOffsetFromAnchorIndex && triangleType == 0) * anchorSuccessorIndex + int(!noOffsetFromAnchorIndex && triangleType == 1) * anchorPredecessorIndex;
    
    return position + halfThickness * (UpFactors[finalIndex] * up + RightFactors[finalIndex] * right);
}
