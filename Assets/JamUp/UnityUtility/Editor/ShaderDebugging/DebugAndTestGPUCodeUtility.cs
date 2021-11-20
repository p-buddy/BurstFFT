using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

using JamUp.StringUtility;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine.Profiling;
using static JamUp.StringUtility.ContextProvider;

namespace JamUp.UnityUtility.Editor
{
    // TODO:
    // - handle multiple shaders of same name
    // - validate inputs
    public static class DebugAndTestGPUCodeUtility
    {
        private static readonly string ParentDirectory = Path.Combine(Application.dataPath, typeof(DebugAndTestGPUCodeUtility).NamespaceAsPath(), "ShaderDebugging");
        private static readonly string GeneratedFilesRootDirectory = Path.Combine(ParentDirectory, "Temp");
        
        #region Generated Text
        private const string GeneratedFileNamePrefix = "ComputeShaderToDebug";
        private const string GeneratedFileExtension = ".compute";
        #endregion Generated Text

        #region GPU File / Language
        private const string CgExtension = ".cginc";
        #endregion

        private static readonly Dictionary<string, List<string>> FullPathToCgFileByName;
        
        static DebugAndTestGPUCodeUtility()
        {
            string[] cgFiles = Directory.GetFiles(Application.dataPath, $"*{CgExtension}", SearchOption.AllDirectories);
            FullPathToCgFileByName = new Dictionary<string, List<string>>(cgFiles.Length);
            foreach (string fullPathToCgFile in cgFiles)
            {
                string cgFileName = Path.GetFileName(fullPathToCgFile).RemoveSubString(CgExtension);
                if (FullPathToCgFileByName.TryGetValue(cgFileName, out List<string> files))
                {
                    files.Add(fullPathToCgFile);
                }
                else
                {
                    FullPathToCgFileByName[cgFileName] = new List<string> {fullPathToCgFile};
                }
            }
        }

        public static void GenerateCgIncFile(string contents,
                                             out string generatedFileName,
                                             out Action onFinishedWithFile)
        {
            Directory.CreateDirectory(GeneratedFilesRootDirectory);
            generatedFileName = $"GeneratedCgIncFile_{DateTime.Now.Ticks}";
            string fullPathToGeneratedFile = Path.Combine(GeneratedFilesRootDirectory, $"{generatedFileName}{CgExtension}");
            using var file = new StreamWriter(File.Create(fullPathToGeneratedFile));
            file.Write(contents);
            FullPathToCgFileByName[generatedFileName] = new List<string>{fullPathToGeneratedFile};
            onFinishedWithFile = () => DeleteTemporaryFile(fullPathToGeneratedFile);
        }
        
        public static void SendToGPUFunctionAndGetOutput<TOutput>(this IGPUFunctionArguments inputArguments,
                                                                  string cgFile,
                                                                  string functionName,
                                                                  out TOutput output)
        {
            string fullPathToCgFile = GetFullPathToCgFile(cgFile, functionName);
            typeof(TOutput).AssertIsValidShaderType();
            ComputeBuffer outputBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(TOutput)));

            BuildAndSetInputBuffersForInput(inputArguments, out ComputeBufferProperty[] inputBuffers);

            ComputeBufferProperty[] allBuffers = new ComputeBufferProperty[inputBuffers.Length + 1];
            allBuffers[0] = new ComputeBufferProperty(ComputeShaderForTesting.OutputBufferVariableName, outputBuffer);
            inputBuffers.CopyTo(allBuffers, 1);
            
            string generatedComputeShaderFileName = $"{GeneratedFileNamePrefix}{functionName}";
            string generatedFileFullPath = Path.Combine(GeneratedFilesRootDirectory, $"{generatedComputeShaderFileName}{GeneratedFileExtension}");

            Directory.CreateDirectory(GeneratedFilesRootDirectory);
            using (var file = new StreamWriter(File.Create(generatedFileFullPath)))
            {
                var functionToTest = new TestableGPUFunction(functionName, fullPathToCgFile, typeof(TOutput), inputArguments);
                string computeShaderContents = ComputeShaderForTesting.BuildNewForFunction(functionToTest);
                file.Write(computeShaderContents);
            }
            
            DispatchDebugComputeShader(functionName, generatedFileFullPath, allBuffers);
            CollectOutput(outputBuffer, out output);

            UpdateNecessaryInputs(inputArguments, inputBuffers);

            foreach (ComputeBufferProperty bufferProperty in allBuffers)
            {
                bufferProperty.Buffer.Dispose();
            }
        }
        
        private static string GetFullPathToCgFile(string cgFile, string functionName)
        {
            string cgFileName = Path.GetFileName(cgFile).RemoveSubString(CgExtension);
            Assert.IsTrue(FullPathToCgFileByName.ContainsKey(cgFileName), $"{Context()}No cg file called '{cgFileName}' found.");
            // TODO handle multiple files of same name; can use function name to check which file contains function; if multiple, should probably alert user that they are making things confusing
            return FullPathToCgFileByName[cgFileName][0];
        }

        private static void BuildAndSetInputBuffersForInput(IGPUFunctionArguments arguments, out ComputeBufferProperty[] inputBuffers)
        { 
            Type[] inputTypes = arguments.GetInputTypes();
            object[] values = arguments.GetInputValues();
            inputBuffers = new ComputeBufferProperty[inputTypes.Length];
            for (var index = 0; index < inputTypes.Length; index++)
            {
                inputTypes[index].AssertIsValidShaderType();
                inputBuffers[index] = new ComputeBufferProperty($"{ComputeShaderForTesting.InputBufferVariableName}{index}", new ComputeBuffer(1, Marshal.SizeOf(inputTypes[index])));
                Array value = Array.CreateInstance(inputTypes[index], 1);
                ((IList)value)[0] = values[index];
                inputBuffers[index].Buffer.SetData(value);
            }
        }

        private static void DispatchDebugComputeShader(string functionName, string generatedFileFullPath, ComputeBufferProperty[] buffers)
        {
            string pathRelativeToProjectFolder = generatedFileFullPath.RemoveSubString(Directory.GetCurrentDirectory()).Remove(0, 1);
            AssetDatabase.ImportAsset(pathRelativeToProjectFolder, ImportAssetOptions.ForceUpdate);
            ComputeShader computeShader = (ComputeShader)AssetDatabase.LoadAssetAtPath(pathRelativeToProjectFolder, typeof(ComputeShader));
            Assert.IsNotNull(computeShader, $"{Context()}Generated compute shader could not be retrieved as an asset. Please go inspect it at: {generatedFileFullPath}");
            int kernelIndex = computeShader.FindKernel($"{ComputeShaderForTesting.KernelPrefix}{functionName}");
            foreach (ComputeBufferProperty bufferProperty in buffers)
            {
                computeShader.SetBuffer(kernelIndex, bufferProperty.PropertyName, bufferProperty.Buffer);
            }
            computeShader.Dispatch(kernelIndex, 1, 1, 1);
            //AssetDatabase.DeleteAsset(pathRelativeToProjectFolder);
        }
        
        private static void CollectOutput<TOutput>(ComputeBuffer outputBuffer, out TOutput output)
        {
            TOutput[] outputArray = new TOutput[1];
            outputBuffer.GetData(outputArray);
            output = outputArray[0];
        }

        private static void UpdateNecessaryInputs(IGPUFunctionArguments inputArguments, ComputeBufferProperty[] inputBuffers)
        {
            InputModifier[] inputModifiers = inputArguments.GetModifiers();
            Type[] inputTypes = inputArguments.GetInputTypes();
            for (int i = 0; i < inputArguments.ArgumentCount; i++)
            {
                if (inputModifiers[i] == InputModifier.Out || inputModifiers[i] == InputModifier.InOut)
                {
                    Array value = Array.CreateInstance(inputTypes[i], 1);
                    inputBuffers[i].Buffer.GetData(value);
                    inputArguments.UpdateInputValueAtIndex(((IList)value)[0], i);
                }
            }
        }

        private static void DeleteTemporaryFile(string fullPathToFile)
        {
            Assert.IsTrue(Path.GetDirectoryName(fullPathToFile) == GeneratedFilesRootDirectory);
            if (File.Exists(fullPathToFile))
            {
                File.Delete(fullPathToFile);
            }

            if (Directory.GetFiles(GeneratedFilesRootDirectory).Length == 0)
            {
                DeleteGeneratedFilesDirectory();
            }
        }
        
        private static void DeleteGeneratedFilesDirectory()
        {
            if (Directory.Exists(GeneratedFilesRootDirectory))
            {
                Directory.Delete(GeneratedFilesRootDirectory, true);
            }

            string metaFile = $"{GeneratedFilesRootDirectory}.meta";
            if (File.Exists(metaFile))
            {
                File.Delete(metaFile);
            }
        }
    }
}