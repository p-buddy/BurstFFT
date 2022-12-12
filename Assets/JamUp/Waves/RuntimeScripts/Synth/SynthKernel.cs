using Unity.Audio;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace JamUp.Waves.RuntimeScripts.Synth
{ 
    [BurstCompile(CompileSynchronously = true)]
    public struct SynthKernel: IAudioKernel<SynthKernel.Parameters, SynthKernel.Providers>
    {
        private const float TransitionTime = 0.01f;

        private Oscillator transition;
        private Oscillator main;

        private CurrentWavesElement waveUpdate;
        private CurrentWaveAxes axesUpdate;
        private float frequencyUpdate;
        private float durationUpdate;

        private bool transitionComplete;

        private bool paused;
        private bool initTransition;
        
        public enum Parameters { }
        public enum Providers { }
        
        public void Initialize()
        {
            paused = true;
            transition = default;
        }

        public void Execute(ref ExecuteContext<Parameters, Providers> context)
        {
            if (paused) return;

            SampleBuffer output = context.Outputs.GetSampleBuffer(0);
            int channelCount = output.Channels;
            int sampleFrames = output.Samples;
            int sampleRate = context.SampleRate;
            
            if (initTransition)
            {
                var activeOscillator = transitionComplete ? main : transition;
                float currentPhase = activeOscillator.Phase;
                float currentFrequency = activeOscillator.RootFrequencyNow;
                
                AllWavesElement transitionStart = activeOscillator.GetCurrentState();
                AllWavesElement transitionEnd = waveUpdate.CaptureToAllWavesElement(0f, axesUpdate);
            
                CurrentWavesElement transitionWave = new(transitionStart, transitionEnd);
                CurrentWaveAxes transitionAxes = new(transitionStart, transitionEnd);

                transition = new Oscillator(transitionWave, 
                                            transitionAxes, 
                                            new float2(currentFrequency, frequencyUpdate), 
                                            TransitionTime,
                                            sampleRate,
                                            currentPhase);

                transitionComplete = false;
                initTransition = false;
            }

            for (int frame = 0; frame < sampleFrames; frame++)
            {
                float value = GetValue(sampleRate);

                for (int channel = 0; channel < channelCount; ++channel)
                {
                    NativeArray<float> channelBuffer = output.GetBuffer(channel);
                    channelBuffer[frame] = math.isnan(value) ? 0f : value;
                }
            }
        }

        private float GetValue(int sampleRate)
        {
            if (transitionComplete) return main.GetMono();
            transitionComplete = transition.DurationComplete;
            if (!transitionComplete) return transition.GetMono();
            main = new Oscillator(waveUpdate, axesUpdate, new float2(frequencyUpdate), durationUpdate, sampleRate, transition.Phase);
            return main.GetMono();
        }

        private void SetWave(CurrentWavesElement wave, CurrentWaveAxes axes, float frequency, float duration)
        {
            waveUpdate = wave;
            axesUpdate = axes;
            frequencyUpdate = frequency;
            durationUpdate = duration;
            initTransition = true;
            paused = false;
        }

        public void Dispose()
        {
        }

        public static void Update<TAudioKernelUpdate>(DSPCommandBlock block,
                                                      DSPNode node,
                                                      TAudioKernelUpdate task)
            where TAudioKernelUpdate : struct, IAudioKernelUpdate<Parameters, Providers, SynthKernel> =>
            block.UpdateAudioKernel<TAudioKernelUpdate, Parameters, Providers, SynthKernel>(task, node);

        public static DSPNode Create(DSPCommandBlock block) => block.CreateDSPNode<Parameters, Providers, SynthKernel>();
        
        public readonly struct Set: IAudioKernelUpdate<Parameters, Providers, SynthKernel>
        {
            private readonly CurrentWavesElement wave;
            private readonly CurrentWaveAxes axis;

            private readonly float rootFrequency;
            private readonly float duration;

            public Set(CurrentWavesElement wave, CurrentWaveAxes axis, float rootFrequency, float duration)
            {
                this.wave = wave;
                this.axis = axis;
                this.rootFrequency = rootFrequency;
                this.duration = duration;
            }

            public void Update(ref SynthKernel audioKernel) => audioKernel.SetWave(wave, axis, rootFrequency, duration);
        }
        
        public struct Pause: IAudioKernelUpdate<Parameters, Providers, SynthKernel>
        {
            public void Update(ref SynthKernel audioKernel) => audioKernel.paused = true;
        }
    }
}