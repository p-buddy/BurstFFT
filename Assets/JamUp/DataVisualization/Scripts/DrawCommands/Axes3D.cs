using System.Security.Cryptography;
using Shapes;
using Camera = UnityEngine.Camera;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.DataVisualization
{
    public struct Axes3D : IDrawCommand
    {
        public int CullingLayer { get; set; }
        
        private float length;

        public Axes3D(float length, int cullingLayer = default)
        {
            this.length = length;
            CullingLayer = cullingLayer;
        }
        
        public void DrawNow(Camera camera)
        {
            using(Draw.Command(camera))
            {
                Draw.LineGeometry = LineGeometry.Volumetric3D;
                Draw.ThicknessSpace = ThicknessSpace.Pixels;
                Draw.Thickness = 4;

                // draw lines
                Draw.Line(float3.zero, math.forward() * length, Color.red);
                Draw.Line(float3.zero, math.up() * length, Color.green);
                Draw.Line(float3.zero, math.right() * length, Color.blue);
            }
        }
    }
}