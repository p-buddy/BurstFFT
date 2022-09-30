using JamUp.UnityUtility;
using UnityEngine;

namespace JamUp.Waves.Scripts
{
    public static class AnimatableShaderPropertyUtility
    {
        public static void Set(this MaterialPropertyBlock block, AnimatableShaderProperty<float> property)
        {
            block.SetInteger(property.Animation.ID, (int)property.Animation.Value);
            block.SetFloat(property.From.ID, property.From.Value);
            block.SetFloat(property.To.ID, property.To.Value);
        }
        
        public static void Set(this MaterialPropertyBlock block, AnimatableShaderProperty<int> property)
        {
            block.SetInteger(property.Animation.ID, (int)property.Animation.Value);
            block.SetInteger(property.From.ID, property.From.Value);
            block.SetInteger(property.To.ID, property.To.Value);
        }

        public static void Set(this MaterialPropertyBlock block, ShaderProperty<float> property)
        {
            block.SetFloat(property.ID, property.Value);
        }

        public static void Set(this MaterialPropertyBlock block, ShaderProperty<int> property)
        {
            block.SetInteger(property.ID, property.Value);
        }
    }
}