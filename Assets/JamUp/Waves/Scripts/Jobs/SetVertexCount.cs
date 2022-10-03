using JamUp.UnityUtility;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace JamUp.Waves.Scripts
{
    public struct SetVertexCount: IJob
    {
        [ReadOnly]
        public NativeArray<AnimatableShaderProperty<float>> SignalTime;
        [ReadOnly]
        public NativeArray<AnimatableShaderProperty<float>> SampleRate;
        [WriteOnly]
        public NativeArray<int> VertexCount;

        public void Execute()
        {
            AnimatableShaderProperty<float> signalLength = SignalTime[0];
            AnimatableShaderProperty<float> sampleRate = SampleRate[0];

            float maxSignalTime = math.max(signalLength.From.Value, signalLength.To.Value);
            float maxSampleRate = math.max(sampleRate.From.Value, sampleRate.To.Value);
            VertexCount[0] = 24 * (int)(maxSignalTime / (1f / maxSampleRate));
        }
    }
}