using pbuddy.TypeScriptingUtility.RuntimeScripts;

namespace JamUp.Waves.RuntimeScripts
{
    public interface IValueSettable<T>: IValuable<T> where T : struct
    {
        T Value { set; }
    }
}