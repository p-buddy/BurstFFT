using System;
using JamUp.UnityUtility.Scripts;
using Unity.Mathematics;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

namespace JamUp.DataVisualization
{
    public class ShapeDrawer : SingletonMonoBehaviour<ShapeDrawer>
    {
        public static bool UseCullingMasks { get; set; } = false;

        public static Camera Camera
        {
            get
            {
                CheckInstance();
                if (Camera.main == null)
                {
                    Camera camera = new GameObject("Camera").AddComponent<Camera>();
                    camera.tag = "MainCamera";
                }

                return Camera.main;
            }
        }

        private static bool CanDraw(int cullingLayer)
        {
            switch (Camera.cameraType)
            {
                case CameraType.Preview:
                case CameraType.Reflection:
                    return
                        false; // Don't render in preview windows or in reflection probes in case we run this script in the editor
            }

            if (UseCullingMasks && (Camera.cullingMask & (1 << cullingLayer)) == 0)
            {
                return false;
            }

            return true;
        }

        public static void SetCamera(float3 position, quaternion rotation)
        {
            Transform transform = Camera.transform;
            transform.position = position;
            transform.rotation = rotation;
        }

        public static void SetCamera(float3 position, float3 lookAtPosition)
        {
            Transform transform = Camera.transform;
            transform.position = position;
            transform.LookAt(lookAtPosition);
        }

        public static void Do<TDrawCommand>(TDrawCommand command) where TDrawCommand : IDrawCommand
        {
            if (CanDraw(command.CullingLayer))
            {
                Camera.onPreRender += command.DrawNow;
                Camera.onPostRender -= command.DrawNow;
            }
        }

        public static void DoForever<TDrawCommand>(TDrawCommand command) where TDrawCommand : IDrawCommand
        {
            if (CanDraw(command.CullingLayer))
            {
                Camera.onPreRender += command.DrawNow;
            }
        }

        public static void DoRepeating<TDrawCommand>(TDrawCommand command, out Action cancel)
            where TDrawCommand : IDrawCommand
        {
            if (CanDraw(command.CullingLayer))
            {
                Camera.onPreRender += command.DrawNow;
                cancel = () => Camera.onPostRender -= command.DrawNow;
            }

            cancel = default;
        }

        #region MonoBehavior

        private void Awake()
        {
            Camera.onPreRender = null;
            Camera.onPostRender = null;
        }

        private void OnDestroy()
        {
            Camera.onPreRender = null;
            Camera.onPostRender = null;
        }

        #endregion
    }
}