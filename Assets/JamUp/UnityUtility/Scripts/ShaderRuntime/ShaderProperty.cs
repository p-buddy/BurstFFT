using UnityEngine;

using pbuddy.StringUtility.RuntimeScripts;

namespace JamUp.UnityUtility
{
    public struct ShaderProperty
    {
        public int ID;

        public ShaderProperty(string name, string toRemove)
        {
            ID = Shader.PropertyToID(name.RemoveSubString(toRemove));
        }
    }
}