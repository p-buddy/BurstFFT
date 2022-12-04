using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.UnityUtility
{
    public static class ShaderPropertyUtility
    {
        private static readonly Dictionary<string, (int, Type)> IdByName = new ();

        public static void RegisterProperty<T>(string name, int id)
        {
            IdByName[name] = (id, typeof(T));
        }
        
        public static string ReadoutAll(this MaterialPropertyBlock block)
        {
            StringBuilder builder = new ();
            foreach (var (name, (id, type)) in IdByName)
            {
                try
                {
                    builder.AppendLine($"{name}: {block.GetPropertyReadout(id, type)}");
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
            return builder.ToString();
        }

        public static void Set(this MaterialPropertyBlock block, ShaderProperty<Matrix4x4> property)
        {
            block.SetMatrix(property.ID, property.Value);
        }
        
        public static void Set(this MaterialPropertyBlock block, ShaderProperty<float> property)
        {
            block.SetFloat(property.ID, property.Value);
        }
        
        public static void Set(this MaterialPropertyBlock block, ShaderProperty<int> property)
        {
            block.SetInteger(property.ID, property.Value);
        }
        
        public static void Set(this MaterialPropertyBlock block, ShaderProperty<Matrix4x4[]> property)
        {
            block.SetMatrixArray(property.ID, property.Value);
        }
        
        public static void Set(this MaterialPropertyBlock block, ShaderProperty<float[]> property)
        {
            block.SetFloatArray(property.ID, property.Value);
        }
        
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
        
        private static string GetPropertyReadout(this MaterialPropertyBlock block, int id, Type type)
        {
            if (type == typeof(float)) return $"{block.GetFloat(id)}";
            if (type == typeof(float[])) return $"{String.Join(", ", block.GetFloatArray(id))}";
            if (type == typeof(Vector4)) return $"{block.GetVector(id)}";
            if (type == typeof(float4)) return $"{block.GetVector(id)}";
            if (type == typeof(Vector4[])) return $"{String.Join("\n", block.GetVectorArray(id))}";
            if (type == typeof(float4[])) return $"{String.Join("\n", block.GetVectorArray(id))}";
            if (type == typeof(Matrix4x4)) return $"{block.GetMatrix(id)}";
            if (type == typeof(Matrix4x4[])) return $"\n{String.Join("\n", block.GetMatrixArray(id))}";
            if (type == typeof(int)) return $"{block.GetInt(id)}";
            if (type.IsEnum) return Enum.ToObject(type, block.GetInt(id)).ToString();

            throw new Exception($"Unhandled property type: {type}");
        }
    }
}