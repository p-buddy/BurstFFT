struct Wave 
{
    float Frequency;
    float Amplitude;
    float PhaseRadians;
    float4 WaveTypeRatio;
};

Wave ConstructWave(const in float frequency, const in float amplitude, const in float phaseRadians, const in float4 waveTypeRatio)
{
    Wave wave;
    wave.Frequency = frequency;
    wave.Amplitude = amplitude;
    wave.PhaseRadians = phaseRadians;
    wave.WaveTypeRatio = waveTypeRatio;
    return wave;
}