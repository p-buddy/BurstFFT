using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace JamUp.UnityUtility.Editor
{
    public static class ShaderProgrammingHelper
    {
        private const string ShaderLanguage = "CG"; // Different for URP & HDRP
        public const string ShaderProgramIdentifier = ShaderLanguage+ "PROGRAM";

        public static readonly Dictionary<Type, string> ShaderTypeNameByManagedType = new Dictionary<Type, string>()
        {
            {typeof(float), "float"},
            {typeof(float2), "float2"},
            {typeof(float3), "float3"},
            {typeof(float4), "float4"},
            {typeof(uint), "uint"},
        };

        public static bool IsConvertibleToShaderType(Type type)
        {
            return ShaderTypeNameByManagedType.ContainsKey(type);
        }

        public static string GetShaderTypeName(Type type)
        {
            return ShaderTypeNameByManagedType[type];
        }

        public static string GetReadOnlyBufferDeclaration(Type type, string variableName)
        {
            return $"StructuredBuffer<{ShaderTypeNameByManagedType[type]}> {variableName};";
        }
    }
}