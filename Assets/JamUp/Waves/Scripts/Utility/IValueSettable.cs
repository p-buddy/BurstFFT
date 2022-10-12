using pbuddy.TypeScriptingUtility.RuntimeScripts;

namespace JamUp.Waves.Scripts
{
    public interface IValueSettable<T>: IValuable<T> where T : struct
    {
        T Value { set; }
    }
}