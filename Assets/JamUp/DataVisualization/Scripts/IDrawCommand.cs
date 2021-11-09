using UnityEngine;

namespace JamUp.DataVisualization
{
    public interface IDrawCommand
    {
        int CullingLayer { set; get; }
        void DrawNow(Camera camera);
    }
}