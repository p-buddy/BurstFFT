namespace JamUp.NativeArrayUtility
{
    public interface IConverter<TFrom, TTo>
    {
        TTo Convert(TFrom value, int index);
    }
}