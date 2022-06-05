using Unity.Mathematics;
using UnityEngine;

namespace JamUp.UnityUtility
{
    public static class ShaderPropertyUtility
    {
        public static void SetProperty<TData>(this MaterialPropertyBlock block, ShaderProperty<TData> property, TData toSet)
        {
            switch (toSet)
            {
                case float f:
                    block.SetFloat(property.ID, f);
                    return;
                case float[] fa:
                    block.SetFloatArray(property.ID, fa);
                    return;
                case Vector4[] va:
                    block.SetVectorArray(property.ID, va);
                    return;
                case int i:
                    block.SetInt(property.ID, i);
                    return;
                case Matrix4x4 m:
                    block.SetMatrix(property.ID, m);
                    return;
            }
        }
    }
}