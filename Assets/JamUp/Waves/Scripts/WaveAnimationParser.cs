using System.Dynamic;
using JamUp.TypescriptGenerator.Scripts;
using JamUp.Waves.Scripts.API;

namespace JamUp.Waves.Scripts
{
    public class WaveAnimationParser
    {
        public void Init(ExpandoObject obj)
        {
            KeyFrame frame = CastToFrame(obj);
        }

        public void AddKeyFrame(ExpandoObject obj)
        {
            KeyFrame frame = CastToFrame(obj);
        }

        private KeyFrame CastToFrame(ExpandoObject obj) => Converter.To<KeyFrame>(obj);
    }
}