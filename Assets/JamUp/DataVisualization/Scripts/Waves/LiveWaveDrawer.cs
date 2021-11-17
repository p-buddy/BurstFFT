using UnityEngine;

namespace JamUp.DataVisualization.Waves
{
    public class LiveWaveDrawer : MonoBehaviour, ILiveDrawer<WaveDrawingState>
    {
        private ObjectForMeshDrawing drawer;

        [SerializeField] 
        private WaveDrawingState state;

        private WaveDrawingState previousState;
        private MeshData? updatedMeshData;
        
        private void Update()
        {
            bool updatePreviousState = false;
            
            if (state.Color != previousState.Color)
            {
                drawer.SetColor(state.Color);
                updatePreviousState = true;
            } 
            
            if (state.StateRequiresMeshRebuild(previousState))
            {
                updatedMeshData = state.MeshDataForState();
                updatePreviousState = true;
            }
            
            previousState = updatePreviousState ? state.Copy() : previousState;
        }

        private void LateUpdate()
        {
            if (updatedMeshData.HasValue)
            {
                DrawAsMesh.DrawMesh(updatedMeshData.Value, in drawer);
                updatedMeshData.Value.Dispose();
                updatedMeshData = null;
            }
        }

        public void SetDrawingObject(in ObjectForMeshDrawing objectForMeshDrawing)
        {
            drawer = objectForMeshDrawing;
        }

        public void SetInitialState(WaveDrawingState initialState)
        {
            state = initialState;
        }
    }
}