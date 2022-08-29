using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace JamUp.Waves.Scripts.Camera
{
    public struct InitMatrixJob : IJob
    {
        [ReadOnly]
        public Projection Projection;
        
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
            switch (Projection)
            {
                case Projection.Orthographic:
                    float horizontal = Settings.OrthographicSize * aspect;
                    Result[0] = float4x4.OrthoOffCenter(horizontal, 
                                                        -horizontal,
                                                        Settings.OrthographicSize,
                                                        -Settings.OrthographicSize,
                                                        Settings.NearClippingPlane,
                                                        Settings.FarClippingPlane);
                    return;
                case Projection.Perspective:
                    Result[0] = float4x4.PerspectiveFov(Settings.VerticalFieldOfView,
                                                        aspect,
                                                        Settings.NearClippingPlane,
                                                        Settings.FarClippingPlane);
                    return;

            }
        }
    }
}