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

namespace JamUp.ShaderUtility.Editor
{
    // TODO:
    // - handle multiple shaders of same name
    // - validate inputs
    public static class DebugAndTestGPUCodeUtility
    {
        private static readonly string GeneratedComputeShadersDirectory = FileGenerator.GetPathToSubDirectory("ComputeShaders");
        private static readonly string GeneratedCgFilesDirectory = FileGenerator.GetPathToSubDirectory("CgInclude");
        
        #region Generated Text
        private const string GeneratedFileNamePrefix = "ComputeShaderToDebug";
        private const string GeneratedFileExtension = ".compute";
        #endregion Generated Text

        #region GPU File / Language
        private const string CgExtension = ".cginc";
        #endregion
        
        private static readonly Dictionary<string, string> FullPathToCgFileByName;
        private static readonly Dictionary<int, string> GeneratedCgFileByHash;
        private static readonly Dictionary<TestableGPUFunction, ComputeShader> ComputeShaderByFunctionItTests;

        static DebugAndTestGPUCodeUtility()
        {
            #region Collect Project's Cg Files
            string[] cgFiles = Directory.GetFiles(Application.dataPath, $"*{CgExtension}", SearchOption.AllDirectories);
            FullPathToCgFileByName = new Dictionary<string, string>(cgFiles.Length);
            foreach (string fullPathToCgFile in cgFiles)
            {
                string cgFileName = Path.GetFileName(fullPathToCgFile).RemoveSubString(CgExtension);
                Assert.IsFalse(FullPathToCgFileByName.ContainsKey(cgFileName), $"{Context()}The {nameof(DebugAndTestGPUCodeUtility)} requires all {CgExtension} files be named uniquely, but there is more than one file named '{cgFileName}'. Please remove it to avoid errors.");
                FullPathToCgFileByName[cgFileName] = fullPathToCgFile;
            }
            #endregion Collect Project's Cg Files

            #region Collect (Already) Generated Cg Files
            if (Directory.Exists(GeneratedCgFilesDirectory))
            {
                string[] generatedCgFiles = Directory.GetFiles(GeneratedCgFilesDirectory);
                GeneratedCgFileByHash = new Dictionary<int, string>(generatedCgFiles.Length);
                foreach (string cgFile in generatedCgFiles)
                {
                    int hash = File.ReadAllText(cgFile).GetHashCode();
                    GeneratedCgFileByHash[hash] = cgFile;
                }
            }
            GeneratedCgFileByHash ??= new Dictionary<int, string>();
            #endregion Collect (Already) Generated Cg Files

            #region Collect (Already) Generated Compute Shaders
            if (Directory.Exists(GeneratedComputeShadersDirectory))
            {
                string[] computeShaderFiles = Directory.GetFiles(GeneratedComputeShadersDirectory)
                                                       .Where(file => !Path.GetExtension(file).Contains("meta"))
                                                       .ToArray();
                ComputeShaderByFunctionItTests = new Dictionary<TestableGPUFunction, ComputeShader>(computeShaderFiles.Length);
                foreach (string preBuiltComputeShader in computeShaderFiles)
                {
                    string pathRelativeToProjectFolder = preBuiltComputeShader.GetPathRelativeToProject();
                    if (ComputeShaderForTesting.TryGetTestedFunctionFromFile(preBuiltComputeShader, out TestableGPUFunction key) &&
                        File.Exists(key.FullPathToFileContainingFunction))
                    {
                        var value = (ComputeShader)AssetDatabase.LoadAssetAtPath(pathRelativeToProjectFolder, typeof(ComputeShader));
                        ComputeShaderByFunctionItTests[key] = value;
                    }
                    else
                    {
                        AssetDatabase.DeleteAsset(pathRelativeToProjectFolder);
                        FileGenerator.DeleteGeneratedFile(preBuiltComputeShader);
                    }
                }
            }
            ComputeShaderByFunctionItTests ??= new Dictionary<TestableGPUFunction, ComputeShader>();
            #endregion Collect (Already) Generated Compute Shaders
        }
        
        public static void GenerateCgIncFile(string contents,
                                             out string generatedFileName)
        {
            Directory.CreateDirectory(GeneratedCgFilesDirectory);
            int hash = contents.GetHashCode();
            
            if (GeneratedCgFileByHash.TryGetValue(hash, out string path))
            {
                generatedFileName = path;
                return;
            }
            
            generatedFileName = $"GeneratedCgIncludeFile_{hash}";
            string fullPathToGeneratedFile = Path.Combine(GeneratedCgFilesDirectory, $"{generatedFileName}{CgExtension}");
            using var file = new StreamWriter(File.Create(fullPathToGeneratedFile));
            file.Write(contents);
            FullPathToCgFileByName[generatedFileName] = fullPathToGeneratedFile;
        }

        public static void SendToCgFunctionAndGetOutput<TOutput>(this IGPUFunctionArguments inputArguments, 
                                                                 string cgFile,
                                                                 string functionName,
                                                                 out TOutput output)
        {
            typeof(TOutput).AssertIsValidShaderType();
            string fullPathToCgFile = GetFullPathToCgFile(cgFile);
            var functionToTest = new TestableGPUFunction(functionName, fullPathToCgFile, typeof(TOutput), inputArguments);

            var outputBuffer = new ComputeBufferProperty(ComputeShaderForTesting.OutputBufferVariableName,
                                                         new ComputeBuffer(1, Marshal.SizeOf(typeof(TOutput))));

            BuildAndSetInputBuffersForInput(inputArguments, out ComputeBufferProperty[] inputBuffers);
            List<ComputeBufferProperty> allBuffers = inputBuffers.Concat(new []{outputBuffer}).ToList();

            var computeShader = GetDebugComputeShaderToTestFunction(functionToTest);
            DispatchDebugComputeShader(computeShader, allBuffers, functionToTest);
            CollectOutput(in outputBuffer, out output);

            UpdateNecessaryInputs(inputArguments, inputBuffers);
            allBuffers.ForEach(bufferProperty => bufferProperty.Buffer.Dispose());
        }
        
        private static string GetFullPathToCgFile(string cgFile)
        {
            string cgFileName = Path.GetFileName(cgFile).RemoveSubString(CgExtension);
            Assert.IsTrue(FullPathToCgFileByName.ContainsKey(cgFileName), $"{Context()}No cg file called '{cgFileName}' found.");
            return FullPathToCgFileByName[cgFileName];
        }

        private static string GetPathRelativeToProject(this string fullPath)
        {
            return fullPath.RemoveSubString(Directory.GetCurrentDirectory()).Remove(0, 1);
        }

        private static void BuildAndSetInputBuffersForInput(IGPUFunctionArguments arguments, out ComputeBufferProperty[] inputBuffers)
        {
            var individualArguments = arguments.GetArguments();
            Type[] inputTypes = arguments.GetInputTypes();
            Type[] elementTypes = individualArguments.Select(argument => argument.ElementType).ToArray();
            object[] values = arguments.GetInputValues();
            inputBuffers = new ComputeBufferProperty[elementTypes.Length];
            for (var index = 0; index < elementTypes.Length; index++)
            {
                elementTypes[index].AssertIsValidShaderType();
                string propertyName = $"{ComputeShaderForTesting.InputBufferVariableName}{index}";
                ComputeBuffer computeBuffer = new ComputeBuffer(individualArguments[index].ElementLength, Marshal.SizeOf(elementTypes[index]));
                inputBuffers[index] = new ComputeBufferProperty(propertyName, computeBuffer);
                if (inputTypes[index].IsArray)
                {
                    inputBuffers[index].Buffer.SetData(values[index] as Array);
                }
                else
                {
                    Array value = Array.CreateInstance(elementTypes[index], 1);
                    ((IList)value)[0] = values[index];
                    inputBuffers[index].Buffer.SetData(value);
                }
            }
        }

        private static ComputeShader GetDebugComputeShaderToTestFunction(TestableGPUFunction function)
        {
            if (ComputeShaderByFunctionItTests.TryGetValue(function, out ComputeShader computeShader))
            {
                return computeShader;
            }
            
            Directory.CreateDirectory(GeneratedComputeShadersDirectory);
            string generatedComputeShaderFileName = $"{GeneratedFileNamePrefix}{function.FunctionUnderTestName}_{function.GetHashCode()}";
            string generatedFileFullPath = Path.Combine(GeneratedComputeShadersDirectory, $"{generatedComputeShaderFileName}{GeneratedFileExtension}");

            using (var file = new StreamWriter(File.Create(generatedFileFullPath)))
            {
                string computeShaderContents = ComputeShaderForTesting.BuildNewForFunction(function);
                file.Write(computeShaderContents);
            }
            
            string pathRelativeToProjectFolder = generatedFileFullPath.GetPathRelativeToProject();
            AssetDatabase.ImportAsset(pathRelativeToProjectFolder, ImportAssetOptions.ForceUpdate);
            computeShader = (ComputeShader)AssetDatabase.LoadAssetAtPath(pathRelativeToProjectFolder, typeof(ComputeShader));
            Assert.IsNotNull(computeShader, $"{Context()}Generated compute shader could not be retrieved as an asset. Please go inspect it at: {generatedFileFullPath}");
            return computeShader;
        }
        
        private static void DispatchDebugComputeShader(ComputeShader computeShader,
                                                       IEnumerable<ComputeBufferProperty> buffers,
                                                       TestableGPUFunction function)
        {
            int kernelIndex = computeShader.FindKernel($"{ComputeShaderForTesting.KernelPrefix}{function.FunctionUnderTestName}");
            foreach (ComputeBufferProperty bufferProperty in buffers)
            {
                computeShader.SetBuffer(kernelIndex, bufferProperty.PropertyName, bufferProperty.Buffer);
            }
            computeShader.Dispatch(kernelIndex, 1, 1, 1);
        }
        
        private static void CollectOutput<TOutput>(in ComputeBufferProperty outputBuffer, out TOutput output)
        {
            TOutput[] outputArray = new TOutput[1];
            outputBuffer.Buffer.GetData(outputArray);
            output = outputArray[0];
        }

        private static void UpdateNecessaryInputs(IGPUFunctionArguments inputArguments, ComputeBufferProperty[] inputBuffers)
        {
            IGPUFunctionArgument[] arguments = inputArguments.GetArguments();
            for (int i = 0; i < inputArguments.ArgumentCount; i++)
            {
                if (arguments[i].RequiresWriting)
                {
                    if (!arguments[i].IsArray)
                    {
                        Array value = Array.CreateInstance(arguments[i].ElementType, 1);
                        inputBuffers[i].Buffer.GetData(value);
                        inputArguments.UpdateInputValueAtIndex(((IList)value)[0], i);
                    }
                    else
                    {
                        inputBuffers[i].Buffer.GetData((Array)arguments[i].Value);
                    }
                }
            }
        }
    }
}