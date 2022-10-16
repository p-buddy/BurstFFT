struct Wave 
{
    float Frequency;
    float Amplitude;
    float PhaseRadians;
    float4 WaveTypeRatio;
};

Wave ConstructWave(in float frequency, in float amplitude, in float phaseRadians, in float4 waveTypeRatio)
{
    Wave wave;
    wave.Frequency = frequency;
    wave.Amplitude = amplitude;
    wave.PhaseRadians = phaseRadians;
    wave.WaveTypeRatio = waveTypeRatio;
    return wave;
}