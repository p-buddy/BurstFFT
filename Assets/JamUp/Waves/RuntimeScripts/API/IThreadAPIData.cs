using System;
using Unity.Collections;

namespace JamUp.Waves.RuntimeScripts.API
{
    public interface IThreadAPIData
    {
        public NativeList<CreateEntity.PackedFrame> FrameData { get; }
        public NativeList<int> FrameDataOffsets { get; }
        public NativeList<Animatable<WaveState>> WaveStates { get; }
        public NativeList<int> WaveStateOffsets { get; }
        public NativeList<float> RootFrequencies { get; }
        
        public Action<CreateEntity.PackedFrame> PushFrame { get; set; }
        public Action<int> PushFrameOffset { get; set; }
        public Action<int> IncreaseFrameCapacity { get; set; }

        public Action<Animatable<WaveState>> PushWave { get; set; }
        public Action<int> PushPushWave { get; set; }
    }
}