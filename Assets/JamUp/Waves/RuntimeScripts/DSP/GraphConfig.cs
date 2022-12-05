using Unity.Audio;
using UnityEngine;

namespace JamUp.Waves.RuntimeScripts.DSP
{
    public readonly struct GraphConfig
    {
        public SoundFormat Format { get; }
        public int Channels { get; }
        public int BufferLength { get; }
        public int NumBuffers { get; }
        public int SampleRate { get; }

        private GraphConfig(bool forceUseCustomConstructor)
        {
            Format = ChannelEnumConverter.GetSoundFormatFromSpeakerMode(AudioSettings.speakerMode);
            Channels = ChannelEnumConverter.GetChannelCountFromSoundFormat(Format);
            AudioSettings.GetDSPBufferSize(out int bufferLength, out int numBuffers);
            BufferLength = bufferLength;
            NumBuffers = numBuffers;
            SampleRate = AudioSettings.outputSampleRate;
        }

        public static GraphConfig Init() => new (forceUseCustomConstructor: true);
        public DSPGraph CreateGraph() => DSPGraph.Create(Format, Channels, BufferLength, SampleRate);
    }
}