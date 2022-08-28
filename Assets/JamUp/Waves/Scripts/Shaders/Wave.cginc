struct Wave 
{
    float Frequency;
    float Amplitude;
    float PhaseRadians;
    int2 WaveType;
    float TypeRatio;
};

Wave ConstructWave(in float frequency, in float amplitude, in float phaseRadians, in int waveTypeStart, in int waveTypeEnd, in float lerpAmount)
{
    Wave wave;
    wave.Frequency = frequency;
    wave.Amplitude = amplitude;
    wave.PhaseRadians = phaseRadians;
    wave.WaveType = int2(waveTypeStart, waveTypeEnd);
    wave.TypeRatio = lerpAmount;
    return wave;
}