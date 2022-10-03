using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    public interface IShaderPropertyWrapper
    {
        delegate T LerpFromTo<T>(T from, T to, float lerpTime);
        JobHandle Update(int index, bool isLastIndex);

        T Constant<T>();
        AnimatableProperty<T> Animated<T>(float lerpTime, LerpFromTo<T> lerp) where T : new();
    }
}