using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    [BurstCompile]
    public struct CalculateRunningTotalJob
    {
        public struct Int: IJob
        {
            [ReadOnly]
            public NativeArray<int> Input;
        
            [WriteOnly]
            public NativeArray<int> RunningTotal;

            public void Execute()
            {
                var length = Input.Length;
                for (int i = 0; i < length; i++)
                {
                    int previous = i == 0 ? 0 : RunningTotal[i - 1];
                    RunningTotal[i] = previous + Input[i];
                }
            }
        }
        
        public struct Float: IJob
        {
            [ReadOnly]
            public NativeArray<float> Input;
        
            [WriteOnly]
            public NativeArray<float> RunningTotal;

            public void Execute()
            {
                var length = Input.Length;
                for (int i = 0; i < length; i++)
                {
                    float previous = i == 0 ? 0 : RunningTotal[i - 1];
                    RunningTotal[i] = previous + Input[i];
                }
            }
        }
    }
}