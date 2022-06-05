using System;
using System.Dynamic;
using JamUp.Waves;

namespace JamUp.Waves
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

        private KeyFrame CastToFrame(ExpandoObject obj) => Converter.ToKeyFrame(obj);
    }
}