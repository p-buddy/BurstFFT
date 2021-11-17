namespace JamUp.DataVisualization
{
    public interface ILiveDrawer<TState> where TState : struct
    {
        void SetDrawingObject(in ObjectForMeshDrawing objectForMeshDrawing);
        void SetInitialState(TState state);
    }
}