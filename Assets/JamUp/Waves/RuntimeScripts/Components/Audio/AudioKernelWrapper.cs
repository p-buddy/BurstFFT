using Unity.Audio;
using Unity.Entities;

namespace JamUp.Waves.RuntimeScripts.Audio
{
    public readonly struct AudioKernelWrapper: IBufferElementData, IRequiredInArchetype
    {
        public DSPNode Node { get; }
        public AudioKernelWrapper(DSPNode node)
        {
            Node = node;
        }
    }
}