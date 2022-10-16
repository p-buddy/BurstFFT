using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace JamUp.Waves.RuntimeScripts
{
    [BurstCompile]
    public struct MatrixLerpJob : IJob
    {
        [ReadOnly]
        public float4x4 Start;
        
        [ReadOnly]
        public float4x4 End;

        [ReadOnly] 
        public float LerpTime;

        [WriteOnly] 
        public NativeArray<float4x4> Result;
        
        public void Execute()
        {
            Result[0] = Start + (End - Start) * LerpTime;
        }
    }
}