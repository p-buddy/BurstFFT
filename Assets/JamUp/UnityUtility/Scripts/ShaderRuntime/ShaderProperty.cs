using UnityEngine;

using pbuddy.StringUtility.RuntimeScripts;

namespace JamUp.UnityUtility
{
    public readonly struct ShaderProperty<TData> 
    {
        public readonly int ID;

        public ShaderProperty(string name, string toRemove = null)
        {
            ID = Shader.PropertyToID(toRemove is null ? name : name.RemoveSubString(toRemove));
        }
    }
}