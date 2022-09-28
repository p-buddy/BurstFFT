using System.Collections.Generic;
using System.Linq;

namespace JamUp.Waves.Scripts.API
{
    public readonly struct Signal
    {
        public List<KeyFrame> Frames { get; }

        public Signal(params KeyFrame[] frames)
        {
            
            Frames = frames.ToList();
        }

        public void AddFrame(KeyFrame frame) => Frames.Add(frame);

        public void AddFrames(params KeyFrame[] frames) => Frames.AddRange(frames);
    }
}