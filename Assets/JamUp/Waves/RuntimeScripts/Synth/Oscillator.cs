using Unity.Burst;
using Unity.Mathematics;

namespace JamUp.Waves.RuntimeScripts.Synth
{
    [BurstCompile(CompileSynchronously = true)]
    public struct Oscillator
    {
        public float RootFrequencyNow => math.clamp(frequencyTransition.x, frequencyTransition.y, frequencyTransition.LerpBetween(LerpTime));
        public bool DurationComplete => LerpTime >= 1f - frameTime;
        public float Phase => cycleElapsed * CyclesToRadians;

        private readonly float2 frequencyTransition;
        private readonly int framesInDuration;
        private readonly float frameTime;
        private readonly bool valid;
        
        private CurrentWavesElement wave;
        private CurrentWaveAxes axis;

        private int elapsedFrames;
        private float cycleElapsed;

        private float LerpTime => framesInDuration == default ? 0f : (float)elapsedFrames / framesInDuration;

        public Oscillator(CurrentWavesElement wave,
                          CurrentWaveAxes axis,
                          float2 frequencyTransition,
                          float currentDuration,
                          int sampleRate,
                          float phaseOverride = -1f)
        {
            elapsedFrames = 0;
            this.wave = wave;
            this.axis = axis;
            this.frequencyTransition = frequencyTransition;
            cycleElapsed = ToCycles(phaseOverride >= 0f ? phaseOverride : wave.Phase.x);
            
            frameTime = 1f / sampleRate;
            framesInDuration = (int)math.ceil(currentDuration * sampleRate);
            valid = true;
        }

        public float GetMono()
        {
            if (!valid) return 0f;
            
            float lerpTime = LerpTime;
            float value = wave.ValueAtPhase(cycleElapsed * CyclesToRadians, lerpTime);
            float audibleFrequency = frequencyTransition.LerpBetween(lerpTime) * wave.LerpFrequency(lerpTime);
            float cycleEvolution = audibleFrequency * frameTime;

            // still need to account for phase offset caused by overriding the phase
            cycleElapsed += cycleEvolution + wave.PhaseDelta(lerpTime, frameTime) * RadiansToCycles;
            cycleElapsed -= math.floor(cycleElapsed);
            elapsedFrames++;

            return value;
        }

        public AllWavesElement GetCurrentState()
        {
            AllWavesElement state = wave.CaptureToAllWavesElement((float)elapsedFrames / framesInDuration, axis);
            return state;
        }
        
        private const float CyclesToRadians = 2 * math.PI;

        private const float RadiansToCycles = 1 / CyclesToRadians;

        private static float ToCycles(float radians)
        {
            float value = radians * RadiansToCycles;
            value -= math.floor(value);
            return value;
        }
        
        private static float GetInitialPhase(float phase, float frequency, int n) => (n * math.PI - phase) / (2 * math.PI * frequency);
    }
}