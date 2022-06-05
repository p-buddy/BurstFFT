using UnityEngine;

using pbuddy.StringUtility.RuntimeScripts;

namespace JamUp.UnityUtility
{
    public struct ShaderProperty<TData> 
    {
        public int ID;

        public ShaderProperty(string name, string toRemove = null)
        {
            ID = Shader.PropertyToID(toRemove is null ? name : name.RemoveSubString(toRemove));
        }
    }
}