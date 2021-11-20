using UnityEngine;

namespace JamUp.UnityUtility.Editor
{
    public readonly struct ComputeBufferProperty
    {
        public string PropertyName { get; }
        public ComputeBuffer Buffer { get; }
        public ComputeBufferProperty(string propertyName, ComputeBuffer buffer)
        {
            PropertyName = propertyName;
            Buffer = buffer;
        }
    }
}