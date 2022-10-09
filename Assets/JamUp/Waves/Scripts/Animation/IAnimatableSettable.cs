namespace JamUp.Waves.Scripts
{
    public interface IAnimatableSettable: IAnimatable
    {
        AnimationCurve AnimationCurve { set; }

    }
}