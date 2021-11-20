using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

using static JamUp.StringUtility.ContextProvider;

namespace JamUp.UnityUtility.Editor
{
    public static class SupportedShaderTypes
    {
        private static readonly Dictionary<Type, string> ShaderTypeNameByManagedType = new Dictionary<Type, string>()
        {
            // NOTE: Bool is not supported as it is not blittable. A smarter person with the need could figure out what to do...
            {typeof(float), "float"},
            {typeof(float2), "float2"},
            {typeof(float3), "float3"},
            {typeof(float4), "float4"},
            {typeof(uint), "uint"},
            {typeof(int), "int"},
            {typeof(float4x4), "float4x4"},
            {typeof(Matrix4x4), "float4x4"},
        };

        public static readonly Dictionary<string, Type> SupportedManagedTypeByTypeName =
            ShaderTypeNameByManagedType.ToDictionary(pair => pair.Key.Name, pair => pair.Key);
        
        public static bool IsConvertibleToShaderType(this Type type)
        {
            return ShaderTypeNameByManagedType.ContainsKey(type);
        }
        
        public static void AssertIsValidShaderType(this Type type)
        {
            Assert.IsTrue(type.IsConvertibleToShaderType(),
                          $"{Context()}{type} is not a type supported on shaders (or hasn't been added yet!)");
        }
        
        public static string GetShaderTypeName(this Type type)
        {
            return ShaderTypeNameByManagedType[type];
        }

        public static Type LookUpManagedType(string typeName) => SupportedManagedTypeByTypeName[typeName];
    }
}