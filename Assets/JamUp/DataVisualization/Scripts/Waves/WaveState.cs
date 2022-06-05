using UnityEngine;

namespace JamUp.DataVisualization.Waves
{
    public class WaveState : MonoBehaviour
    {
        [SerializeField]
        private WaveDrawingState state;

        public WaveDrawingState State => state;
    }
}