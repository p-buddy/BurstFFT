namespace JamUp.Waves.Scripts
{
    public readonly struct CameraSettings
    {
        public float OrthographicSize { get; }
        public float VerticalFieldOfView { get; }
        public float NearClippingPlane { get; }
        public float FarClippingPlane { get; }
        public static CameraSettings Default => new CameraSettings(5f, 60f, 0.3f, 1000f);
        
        public CameraSettings(float orthographicSize,
                              float verticalFieldOfView,
                              float nearClippingPlane,
                              float farClippingPlane)
        {
            OrthographicSize = orthographicSize;
            VerticalFieldOfView = verticalFieldOfView;
            NearClippingPlane = nearClippingPlane;
            FarClippingPlane = farClippingPlane;
        }
    }
}