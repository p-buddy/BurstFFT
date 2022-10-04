using JamUp.Waves.Scripts.API;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace JamUp.Waves.Scripts
{
    public struct SetCurrentWaves: IJob
    {
        [ReadOnly]
        public int CurrentSettingsIndex;
        
        [ReadOnly]
        public bool IsLast;
        
        public NativeArray<int> CurrentWaveIndex;

        [ReadOnly]
        public NativeArray<int> AllWaveCounts;
        
        [ReadOnly]
        public NativeArray<AnimatableProperty<WaveState>> AllWaves;

        [WriteOnly]
        public NativeArray<float4x4> CurrentWaves;
        
        [WriteOnly]
        public NativeArray<int> CurrentWaveCount;

        public void Execute()
        {
            int startingWaveCount = AllWaveCounts[CurrentSettingsIndex];

            int startingWaveIndex = CurrentWaveIndex[0];
            CurrentWaveIndex[0] = startingWaveIndex + startingWaveCount;

            if (IsLast)
            {
                for (int i = 0; i < startingWaveCount; i++)
                {
                    int index = startingWaveIndex + i;
                    AnimatableProperty<WaveState> state = AllWaves[index];
                    WaveState wave = state.Value;
                    WaveState final = wave.ZeroedAmplitude;
                    CurrentWaves[i] = WaveState.Pack(in wave, in final, (float)state.AnimationCurve);
                }

                CurrentWaveCount[0] = startingWaveCount;
                return;
            }
            
            int endingWaveCount = AllWaveCounts[CurrentSettingsIndex + 1];
            int endingWaveIndex = startingWaveIndex + startingWaveCount;
            int waveCount = System.Math.Max(startingWaveCount, endingWaveCount);
            
            for (int i = 0; i < waveCount; i++)
            {
                int startIndex = startingWaveIndex + i;
                int endIndex = endingWaveIndex + i;

                bool startWaveInvalid = i > startingWaveCount;
                
                WaveState startingWave = startWaveInvalid
                    ? AllWaves[endIndex].Value.ZeroedAmplitude
                    : AllWaves[startIndex].Value;

                AnimationCurve animation = startWaveInvalid ? AnimationCurve.Linear : AllWaves[startIndex].AnimationCurve;

                WaveState endingWave = i > endingWaveCount
                    ? AllWaves[startIndex].Value.ZeroedAmplitude
                    : AllWaves[endIndex].Value;
                
                CurrentWaves[i] = WaveState.Pack(in startingWave, in endingWave, (float)animation);
            }
            
            CurrentWaveCount[0] = waveCount;
        }
    }
}