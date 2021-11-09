using Shapes;
using UnityEngine;

namespace JamUp.DataVisualization
{
    public struct DrawSignalInTime : IDrawCommand
    {
        private enum SignalType
        {
            Real,
            Complex,
        }
        
        public int CullingLayer { get; set; }
        public void DrawNow(Camera camera)
        {
            using (Draw.Command(camera))
            {
                
            }
        }
    }
}