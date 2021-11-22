#include "Wave.cginc"
#include "WaveType.cginc"
#include "Math.cginc"

float TimeToRadians(in float time)
{
    return TwoPI * time;
}

float GetValueAtTime(in Wave wave, in float time)
{
    float rotationAmount = TimeToRadians(time) * wave.Frequency + wave.PhaseRadians;
    float sineValue = sin(rotationAmount);

    float sineFactor = float(SineWaveType & wave.WaveType) * wave.Amplitude * sineValue;
    float squareFactor = float((SquareWaveType & wave.WaveType) >> 1) * wave.Amplitude * sign(sineValue);
    float triangleFactor = float((TriangleWaveType & wave.WaveType) >> 2) * wave.Amplitude * TwoOverPI * asin(sineValue);
    float sawToothFactor = float((SawtoothWaveType & wave.WaveType) >> 3) * wave.Amplitude * -TwoOverPI * atan(1.0f / tan(rotationAmount - PIOverTwo));
    
    return sineFactor + squareFactor + triangleFactor + sawToothFactor;
}

float3 GetDisplacementAtTime(in Wave wave, in float time, in float3 displacementVector, in float3 propagationAxis)
{
    return GetValueAtTime(wave, time) * displacementVector + propagationAxis * time;
}

float GetTimeResolution(in uint sampleRate)
{
    return float(1.0f / sampleRate);
}

float3 GetTangentAtTime(in Wave wave, in float time, in float timeResolution,
                        float3 displacementVector, float3 timeAxis)
{
    float ahead = GetValueAtTime(wave, time + timeResolution);
    float behind = GetValueAtTime(wave, time - timeResolution);
    float slope = (ahead - behind) / (2.0f * TimeToRadians(timeResolution));
    return normalize(slope * displacementVector + timeAxis);
}

struct CoordinateAxes
{
    float3 Right;
    float3 Up;
    float3 Forward;
};

CoordinateAxes GetCoordinateAxes(in float3 forward)
{
    float3 forwardNorm = normalize(forward);
    bool isForwardAskew = abs(forwardNorm.z) != 1.0f;
    float3 up = isForwardAskew ? cross(forwardNorm, float3(0,0,1)) : float3(0, forwardNorm.z, 0);;
    float3 right = cross(up, forward);
    
    CoordinateAxes frame;
    frame.Forward = forward;
    frame.Up = up;
    frame.Right = right;
    return frame;
}

float GetTimeForVertexIndex(in uint vertexIndex, in uint sampleRate)
{
    uint sampleIndex = vertexIndex / 24;
    return float(sampleIndex) / float(sampleRate);
}

Wave SinToCos(in Wave wave)
{
    Wave cosWave;
    cosWave.Amplitude = wave.Amplitude;
    cosWave.Frequency = wave.Frequency;
    cosWave.PhaseRadians = wave.PhaseRadians + PIOverTwo;
    cosWave.WaveType = wave.WaveType;
    return cosWave;
}

struct DebugVertexPosition
{
    uint VertexIndexWithinSection; // 0 - 23
    uint SequenceIndexWithinTriangle; // 0 - 2
    uint TriangleNumber; // 0 - 7
    uint TriangleType; // 0 or 1
    uint AnchorIndex; // 0 - 3
    uint AnchorPredecessorIndex; // 0 - 3
    uint AnchorSuccessorIndex; // 0 - 3
    float3 Up;
    float3 Right;
    uint FinalIndex;
};

float3 GetVertexPosition(in uint vertexIndex,
    in float3 samplePosition,
    in float3 nextSamplePosition,
    in float3 sampleTangent,
    in float3 nextSampleTangent,
    in float thickness/*,
    out DebugVertexPosition debug*/)
{
    // TRIANGLE TYPE 0: [0] (front_face) face vertex # --> [1] (front_face) face vertex # + 1 --> [2] (back_face) face vertex #
    // TRIANGLE TYPE 1: [0] (front_face) face vertex # --> [1] (back_face) face vertex # --> [2] (front_face) face vertex # - 1
    // NOTE: sequence element [0] will be referred to as the "Anchor Index" for a given triangle
    
    // (TYPE 0) triangle 0: (front face) vertex 0 --> (front face) vertex 1 --> (back face) vertex 0 [0-1-2]
    // (TYPE 0) triangle 1: (front face) vertex 1 --> (front face) vertex 2 --> (back face) vertex 1 [3-4-5]
    // (TYPE 0) triangle 2: (front face) vertex 2 --> (front face) vertex 3 --> (back face) vertex 2 [6-7-8]
    // (TYPE 0) triangle 3: (front face) vertex 3 --> (front face) vertex 0 --> (back face) vertex 3 [9-10-11]

    // (TYPE 1) triangle 4: (front face) vertex 0 --> (back face) vertex 0 --> (back face) vertex 3 [12-13-14]
    // (TYPE 1) triangle 5: (front face) vertex 1 --> (back face) vertex 1 --> (back face) vertex 0 [15-16-17]
    // (TYPE 1) triangle 6: (front face) vertex 2 --> (back face) vertex 2 --> (back face) vertex 1 [18-19-20]
    // (TYPE 1) triangle 7: (front face) vertex 3 --> (back face) vertex 3 --> (back face) vertex 2 [21-22-23]
    
    static int UpFactors[4] = {1, 1, -1, -1};
    static int RightFactors[4] = {1, -1, -1, 1};
    
    CoordinateAxes frontAxes = GetCoordinateAxes(sampleTangent);
    CoordinateAxes backAxes = GetCoordinateAxes(nextSampleTangent);

    float halfThickness = 0.5f * thickness;

    uint vertexIndexWithinSection = vertexIndex % 24; // 0 - 23
    uint sequenceIndexWithinTriangle = vertexIndexWithinSection % 3; // 0 - 2
    uint triangleNumber = vertexIndexWithinSection / 3; // 0 - 7
    uint triangleType = triangleNumber / 4; // 0 or 1
    uint anchorIndex = triangleNumber % 4; // 0 - 3
    uint anchorPredecessorIndex = anchorIndex > 0 ? (anchorIndex - 1) % 4 : 3; // 0 - 3
    uint anchorSuccessorIndex = (anchorIndex + 1) % 4; // 0 - 3
    
    bool useFrontAxes = sequenceIndexWithinTriangle == 0 || sequenceIndexWithinTriangle == 1 && triangleType == 0;
    float3 up = float(useFrontAxes) * frontAxes.Up + float(!useFrontAxes) * backAxes.Up;
    float3 right = float(useFrontAxes) * frontAxes.Right + float(!useFrontAxes) * backAxes.Right;
    float3 position = float(useFrontAxes) * samplePosition + float(!useFrontAxes) * nextSamplePosition;

    bool noOffsetFromAnchorIndex = sequenceIndexWithinTriangle == 0 || triangleType == 0 && sequenceIndexWithinTriangle == 2 || triangleType == 1 && sequenceIndexWithinTriangle == 1;
    uint finalIndex = int(noOffsetFromAnchorIndex) * anchorIndex + int(!noOffsetFromAnchorIndex && triangleType == 0) * anchorSuccessorIndex + int(!noOffsetFromAnchorIndex && triangleType == 1) * anchorPredecessorIndex;
    /*
    debug.VertexIndexWithinSection = vertexIndexWithinSection;
    debug.SequenceIndexWithinTriangle = sequenceIndexWithinTriangle;
    debug.TriangleNumber = triangleNumber;
    debug.TriangleType = triangleType;
    debug.AnchorIndex = anchorIndex;
    debug.AnchorPredecessorIndex = anchorPredecessorIndex;
    debug.AnchorSuccessorIndex = anchorSuccessorIndex;
    debug.Up = up;
    debug.Right = right;
    debug.FinalIndex = finalIndex;
    */
    return position + halfThickness * (UpFactors[finalIndex] * up + RightFactors[finalIndex] * right);
}


void AppendPositionAndTangent(in float time, in float timeResolution, in Wave wave,in float3 forward, in float3 displacementAxis, inout float3 position,inout float3 nextPosition, inout float3 tangent, inout float3 nextTangent)
{
    position += GetDisplacementAtTime(wave, time, displacementAxis, forward);
    nextPosition += GetDisplacementAtTime(wave, time + timeResolution, displacementAxis, forward);
          
    tangent += GetTangentAtTime(wave, time, timeResolution, displacementAxis, forward);
    nextTangent += GetTangentAtTime(wave, time + timeResolution, timeResolution, displacementAxis, forward);
}

/*
static int MaxNumberOfWaves = 10;
float3 GetWorldPositionFromCombinedWaves(in uint vertexIndex, in uint sampleRate,
                                         in float4x4 waveOriginToWorldMatrix, in int numberOfWaves,
                                         in float3 displacementAxes[MaxNumberOfWaves], in float frequencies[MaxNumberOfWaves],
                                         in float amplitudes[MaxNumberOfWaves], in float phases[MaxNumberOfWaves],
                                         in uint waveTypes[MaxNumberOfWaves], in float thickness)
{
    float time = GetTimeForVertexIndex(vertexIndex, sampleRate);
    float timeResolution = GetTimeResolution(sampleRate);
    float3 forward = mul(waveOriginToWorldMatrix, float3(0, 0, 1));

    float3 position, nextPosition;
    float3 tangent, nextTangent;
    for (int index = 0; index < numberOfWaves; index++)
    {
        float3 displacementAxis = displacementAxes[index].xyz;
        Wave wave = ConstructWave(frequencies[index], amplitudes[index], phases[index], waveTypes[index]);
        AppendPositionAndTangent(time, timeResolution, wave, forward, displacementAxis, position, nextPosition, tangent, nextTangent);
    }
    tangent = normalize(tangent);
    nextTangent = normalize(nextTangent);
    position = position / float(numberOfWaves);
    nextPosition = nextPosition / float(numberOfWaves);
    float4 position4D = position.xyzx;
    float4 nextPosition4D = nextPosition.xyzx;
    position4D.w = nextPosition4D.w = 1.0f;
    
    position = mul(waveOriginToWorldMatrix, position4D);
    nextPosition = mul(waveOriginToWorldMatrix, nextPosition4D);
    tangent = mul(waveOriginToWorldMatrix, tangent);
    nextTangent = mul(waveOriginToWorldMatrix, nextTangent);
        
    return GetVertexPosition(vertexIndex, position, nextPosition, tangent, nextTangent, thickness);
}
*/