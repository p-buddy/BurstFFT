using Unity.Collections;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public struct CollectTimesBasedOnDurations: IJob
    {
        [ReadOnly]
        public float LastDuration;

        [ReadOnly]
        public NativeArray<float> Durations;
        
        [WriteOnly]
        public NativeArray<float> Times;
        
        public void Execute()
        {
            int length = Times.Length;
            for (int i = 2; i < length; i++)
            {
                float time = i < length - 1 
                    ? Times[i - 1] + Durations[i - 2] 
                    : Times[i - 1] + LastDuration;
                Times[i] = time;
            }
        }
    }
}