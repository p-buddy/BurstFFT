using Unity.Entities;

namespace JamUp.Waves.Scripts
{
    public readonly struct Projection: IComponentData, IAnimatable  
    {
        public ProjectionType ProjectionStart { get; }
        public ProjectionType ProjectionEnd { get; }
        public AnimationCurve Animation { get; }
    }
}