using JamUp.UnityUtility;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.DataVisualization
{
    public static class CameraAccess
    {
        /// <summary>
        /// 
        /// </summary>
        public static Camera Camera
        {
            get
            {
                if (Camera.main == null)
                {
                    Camera camera = new GameObject("Camera").AddComponent<Camera>();
                    camera.gameObject.AddComponent<CameraController>();
                    camera.tag = "MainCamera";
                }

                return Camera.main;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="lookAtPosition"></param>
        public static void SetCamera(float3 position, float3 lookAtPosition)
        {
            Transform transform = Camera.transform;
            transform.position = position;
            transform.LookAt(lookAtPosition);
        }
    }
}