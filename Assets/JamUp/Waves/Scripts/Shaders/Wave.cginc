struct Wave 
{
    float Frequency;
    float Amplitude;
    float PhaseRadians;
    int WaveType; 
};

Wave NewWave(const float frequency, const float amplitude, const float phase_radians, const int wave_type)
{
    Wave wave;
    wave.Frequency = frequency;
    wave.Amplitude = amplitude;
    wave.PhaseRadians = phase_radians;
    wave.WaveType = wave_type;
    return wave;
}