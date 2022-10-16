using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace JamUp.Waves.RuntimeScripts
{
    [BurstCompile]
    public struct InitMatrixJob : IJob
    {
        [ReadOnly]
        public ProjectionType ProjectionType;
        
        [ReadOnly] 
        public CameraSettings Settings;
        
        [ReadOnly]
        public int Width;
        
        [ReadOnly]
        public int Height;
        
        [WriteOnly] 
        public NativeArray<float4x4> Result;

        public void Execute()
        {
            float aspect = (float)Width / Height;
            switch (ProjectionType)
            {
                case ProjectionType.Orthographic:
                    float horizontal = Settings.OrthographicSize * aspect;
                    Result[0] = float4x4.OrthoOffCenter(horizontal, 
                                                        -horizontal,
                                                        Settings.OrthographicSize,
                                                        -Settings.OrthographicSize,
                                                        Settings.NearClippingPlane,
                                                        Settings.FarClippingPlane);
                    return;
                case ProjectionType.Perspective:
                    Result[0] = float4x4.PerspectiveFov(Settings.VerticalFieldOfView,
                                                        aspect,
                                                        Settings.NearClippingPlane,
                                                        Settings.FarClippingPlane);
                    return;

            }
        }
    }
}