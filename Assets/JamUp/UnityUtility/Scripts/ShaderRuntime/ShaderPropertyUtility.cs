using System.Linq;
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
                case Vector4 v:
                    block.SetVector(property.ID, v);
                    return;
                case float4 f4:
                    block.SetVector(property.ID, f4);
                    return;
                case Vector4[] va:
                    block.SetVectorArray(property.ID, va);
                    return;
                case float4[] f4a:
                    block.SetVectorArray(property.ID, f4a.ToList().Select(f4 => (Vector4)f4).ToArray());
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