namespace JamUp.Waves.Scripts.Camera
{
    public readonly struct CameraSettings
    {
        public float OrthographicSize { get; }
        public float VerticalFieldOfView { get; }
        public float NearClippingPlane { get; }
        public float FarClippingPlane { get; }

        public CameraSettings(float orthographicSize = 5f,
                              float verticalFieldOfView = 60f,
                              float nearClippingPlane = 0.3f,
                              float farClippingPlane = 100f)
        {
            OrthographicSize = orthographicSize;
            VerticalFieldOfView = verticalFieldOfView;
            NearClippingPlane = nearClippingPlane;
            FarClippingPlane = farClippingPlane;
        }
    }
}