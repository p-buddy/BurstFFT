using System;
using System.Collections.Generic;
using System.Linq;

namespace JamUp.Waves.RuntimeScripts.API
{
    public readonly struct Signal
    {
        public float RootFrequency { get; }
        public List<KeyFrame> Frames { get; }
        
        public Signal(float rootFrequency, params KeyFrame[] frames)
        {
            RootFrequency = rootFrequency;
            Frames = frames is null || frames.Length == 0 ? new List<KeyFrame>() : frames.ToList();
        }

        
        public void AddFrame(KeyFrame frame)
        {
            Frames?.Add(frame);
        }

        public void AddFrames(params KeyFrame[] frames) => Frames?.AddRange(frames);
    }
}