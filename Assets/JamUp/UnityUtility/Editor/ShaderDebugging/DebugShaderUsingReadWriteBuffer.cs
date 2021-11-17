using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static JamUp.StringUtility.ContextProvider;

namespace JamUp.UnityUtility.Editor
{
    public struct DebugShaderUsingReadWriteBuffer<T> : IDisposable where T : struct
    {
        private readonly ComputeBuffer gpuBuffer;
        private readonly int size;
        
        private NativeArray<T> tempBuffer;
        
        public DebugShaderUsingReadWriteBuffer(int size, Material material, string bufferName)
        {
            this.size = size;
            gpuBuffer = new ComputeBuffer(size, Marshal.SizeOf(typeof(T)));
            tempBuffer = default;
            if (IsShaderReadyForDebugging(material.shader, bufferName))
            {
                material.SetBuffer(bufferName, gpuBuffer);
            }
        }

        public NativeArray<T> GetDataBlocking()
        {
            using (tempBuffer = new NativeArray<T>(size, Allocator.Temp))
            {
                AsyncGPUReadback.RequestIntoNativeArray(ref tempBuffer, gpuBuffer, OnRequestFinished);
                AsyncGPUReadback.WaitAllRequests();
                NativeArray<T> toReturn = new NativeArray<T>(tempBuffer, Allocator.Persistent);
                return toReturn;
            }
        }
        
        public void Dispose()
        {
            gpuBuffer?.Dispose();
        }

        private void OnRequestFinished(AsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                throw new Exception($"AsyncGPUReadback.RequestIntoNativeArray");
            }
        }

        private bool IsShaderReadyForDebugging(Shader shader, string bufferName)
        {
            if (!ShaderProgrammingHelper.ShaderTypeNameByManagedType.TryGetValue(typeof(T), out string shaderTypeName))
            {
                Debug.LogError($"{Context()}Unsupported type for debug buffer: {typeof(T)}");
                return false;
            }
            
            string bufferDeclaration = $"RWStructuredBuffer<{shaderTypeName}> {bufferName};";
            const string debugBlockIdentifier = "/* BEGIN DECLARATION OF DEBUG BUFFERS */";
            const string debugBlockClose = "/* END DECLARATION OF DEBUG BUFFERS */";
            
            string assetPath = Path.GetFullPath(AssetDatabase.GetAssetPath(shader));

            List<string> lines = File.ReadAllLines(assetPath).ToList();
            if (lines.Any(line => line.Contains(bufferDeclaration)))
            {
                return true;
            }

            int GetIndexOfLineContainingString(string matchString)
            {
                var matchingLines = lines.Select((line, index) => new { Line = line, Index = index })
                                         .Where(lineAndIndex => lineAndIndex.Line.Contains(matchString))
                                         .ToList();
                return matchingLines.Any() ? matchingLines.First().Index : -1;
            }

            string GetIndentationForLine(int lineIndex, string matchString)
            {
                string line = lines[lineIndex];
                return line.Substring(0, line.IndexOf(matchString, StringComparison.Ordinal));
            }

            void WriteOutDataAndLogError()
            {
                using var file = new StreamWriter(File.Create(assetPath));
                file.Write(String.Join(Environment.NewLine, lines));
                Debug.LogError($"{Context()}The debug buffer '{bufferName}' was added to {shader.name} underneath the {ShaderProgrammingHelper.ShaderProgramIdentifier} declaration. Restart the scene now.");
            }
            
            if (GetIndexOfLineContainingString(debugBlockIdentifier) is int indexOfDebugBlock && indexOfDebugBlock >= 0)
            {
                string spacing = GetIndentationForLine(indexOfDebugBlock, debugBlockIdentifier);
                lines.Insert(indexOfDebugBlock + 1, $"{spacing}{bufferDeclaration}");
                WriteOutDataAndLogError();
                return false;
            }
            
            if (GetIndexOfLineContainingString(ShaderProgrammingHelper.ShaderProgramIdentifier) is int indexOfCg && indexOfCg >= 0)
            {
                string spacing = GetIndentationForLine(indexOfCg, ShaderProgrammingHelper.ShaderProgramIdentifier);
                int additionIndex = indexOfCg + 1;
                lines.Insert(additionIndex, String.Empty);
                lines.Insert(additionIndex, $"{spacing}{debugBlockClose}");
                lines.Insert(additionIndex, $"{spacing}{bufferDeclaration}");
                lines.Insert(additionIndex, $"{spacing}{debugBlockIdentifier}");
                lines.Insert(additionIndex, String.Empty);
                WriteOutDataAndLogError();
                return false;
            }

            Debug.LogError($"{Context()}The debug buffer '{bufferName}' was not found in {shader.name} and it could not be added automatically.");
            return false;
        }
    }
}