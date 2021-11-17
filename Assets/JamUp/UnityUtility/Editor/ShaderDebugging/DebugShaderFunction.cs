using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Assertions;

using JamUp.StringUtility;
using static JamUp.StringUtility.ContextProvider;

namespace JamUp.UnityUtility.Editor
{
    public readonly struct DebugShaderFunction<TOutput> : IDisposable where TOutput : struct
    {
        private static readonly string ParentDirectory = Path.Combine(Path.Combine(Application.dataPath, "JamUp", "UnityUtility"), "Editor", "ShaderDebugging");
        
        #region Template Files
        private const string SupportingFilesDirectory = "TEMPLATES";
        private const string TemplateFile = "COMPUTE_SHADER_TO_DEBUG_SHADER_FUNCTION_TEMPLATE.compute";
        #endregion Template Files

        #region Sections
        private const string SectionToDeleteOpen = "BEGIN DELETE SECTION";
        private const string SectionToDeleteClose = "END DELETE SECTION";

        private const string InputSectionOpen = "BEGIN INPUT SECTION";
        private const string InputSectionClose = "END INPUT SECTION";
        #endregion Sections

        #region Template Identifiers
        private const string OutputTypeIdentifier = "OUTPUT_TYPE";
        private const string FunctionToDebugIdentifier = "FUNCTION_TO_DEBUG";
        private const string ShaderFilePathIdentifier = "FULL_PATH_TO_FILE_CONTAING_FUNCTION_TEMPLATE";
        private const string InputArgumentsIdentifier = "INPUT_ARGUMENTS";
        #endregion Template Identifiers
        
        #region Generated Text
        private const string InputVariableName = "Input";
        private const string GeneratedFileName = "ComputeShaderToDebug.compute";
        private const string GeneratedFilesDirectory = "Temp";
        #endregion Generated Text
        
        private readonly ComputeBuffer outputBuffer;
        private readonly Type[] inputTypes;
        private readonly ComputeBuffer[] inputBuffers;
        private readonly string generatedFilePath;
        
        public DebugShaderFunction(string fullPathToShaderFile, string functionName, IShaderFunctionInput input, out TOutput output)
        {
            AssertIsValidShaderType(typeof(TOutput));
            outputBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(TOutput)));

            inputTypes = input.GetInputTypes();
            object[] values = input.GetInputValues();
            inputBuffers = new ComputeBuffer[inputTypes.Length];
            for (var index = 0; index < inputTypes.Length; index++)
            {
                AssertIsValidShaderType(inputTypes[index]);
                inputBuffers[index] = new ComputeBuffer(1, Marshal.SizeOf(inputTypes[index]));
                Array value = Array.CreateInstance(inputTypes[index], 1);
                Type t = values[index].GetType();
                ((IList)value)[0] = values[index];
                inputBuffers[index].SetData(value);
            }
            
            Directory.CreateDirectory(Path.Combine(ParentDirectory, GeneratedFilesDirectory));
            generatedFilePath = Path.Combine(ParentDirectory, GeneratedFilesDirectory, $"{GeneratedFileName}{functionName}");
            using var file = new StreamWriter(File.Create(generatedFilePath));
            file.Write(BuildComputeShaderForDebugging(fullPathToShaderFile, functionName));
            
            TOutput[] outputArray = new TOutput[1];
            outputBuffer.GetData(outputArray);
            output = outputArray[0];
        }

        private string BuildComputeShaderForDebugging(string fullPathToShaderFile, string functionName)
        {
            List<string> lines = File.ReadAllLines(Path.Combine(ParentDirectory, SupportingFilesDirectory, TemplateFile)).ToList();
            lines.RemoveSection(new Section(SectionToDeleteOpen, SectionToDeleteClose));
            lines.ReplaceTemplates(new TemplateToReplace(OutputTypeIdentifier, ShaderProgrammingHelper.GetShaderTypeName(typeof(TOutput))),
                                   new TemplateToReplace(FunctionToDebugIdentifier, functionName),
                                   new TemplateToReplace(ShaderFilePathIdentifier, fullPathToShaderFile),
                                   new TemplateToReplace(InputArgumentsIdentifier, GetInputArguments()));
            lines.AddToEndOfSection(new Section(InputSectionOpen, InputSectionClose), GetInputBufferDeclarations());
            return String.Join(Environment.NewLine, lines);
        }

        private string GetInputArguments()
        {
            string[] inputArguments = new string[inputTypes.Length];
            for (var index = 0; index < inputArguments.Length; index++)
            {
                inputArguments[index] = $"{InputVariableName}{index}[0]";
            }
            return String.Join(",", inputArguments);
        }
        
        private string[] GetInputBufferDeclarations()
        {
            string GetDeclaration(Type type, int index)
            {
                return ShaderProgrammingHelper.GetReadOnlyBufferDeclaration(type, $"{InputVariableName}{index}");
            }
            return inputTypes.ToList().Select(GetDeclaration).ToArray();
        }

        private static void AssertIsValidShaderType(Type type)
        {
            Assert.IsTrue(ShaderProgrammingHelper.IsConvertibleToShaderType(type), $"{Context()}{typeof(Type)} is not a type supported on shaders (or hasn't been added yet!)");
        }
        
        public void Dispose()
        {
            outputBuffer.Dispose();
            foreach (ComputeBuffer inputBuffer in inputBuffers)
            {
                inputBuffer.Dispose();
            }
            File.Delete(generatedFilePath);
        }
    }
}