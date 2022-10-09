using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public struct SetAnimationElement<TData, TComponent>: IJob where TData : new()
    {
        public Animatable<TData> From;
        public TData To;

        public void Execute()
        {
            throw new System.NotImplementedException();
        }
    }
}