namespace JamUp.Waves.RuntimeScripts
{
    public interface IAnimatableSettable: IAnimatable
    {
        AnimationCurve AnimationCurve { set; }

    }
}