struct Wave 
{
    float Frequency;
    float Amplitude;
    float PhaseRadians;
    int WaveType; 
};

Wave ConstructWave(in float frequency, in float amplitude, in float phaseRadians, in int waveType)
{
    Wave wave;
    wave.Frequency = frequency;
    wave.Amplitude = amplitude;
    wave.PhaseRadians = phaseRadians;
    wave.WaveType = waveType;
    return wave;
}