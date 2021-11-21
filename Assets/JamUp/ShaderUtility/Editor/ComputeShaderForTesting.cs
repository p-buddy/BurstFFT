using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JamUp.StringUtility;
using UnityEngine;
using UnityEngine.Assertions;

using static JamUp.StringUtility.ContextProvider;

namespace JamUp.ShaderUtility.Editor
{
    public static class ComputeShaderForTesting
    {
        public static bool TryGetTestedFunctionFromFile(string fullPathToFile, out TestableGPUFunction testableGPUFunction)
        {
            List<string> lines = File.ReadLines(fullPathToFile).ToList();
            string saveData = String.Join("", lines.GetSection(SaveDataSection));
            try
            {
                testableGPUFunction = TestableGPUFunction.FromSaveData(saveData);
                return testableGPUFunction != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{Context()}Failed to construct {nameof(TestableGPUFunction)} from {Path.GetFileName(fullPathToFile)}. The following exception was thrown: {e}");
                testableGPUFunction = default;
                return false;
            }
        }
        
        public static string BuildNewForFunction(TestableGPUFunction functionToTest)
        {
            List<string> lines = TemplateLinesForEditing;
            TemplateToReplace[] replacements =
            {
                new TemplateToReplace(OutputTypeIdentifier, functionToTest.OutputType.GetShaderTypeName()),
                new TemplateToReplace(FunctionToDebugIdentifier, functionToTest.FunctionUnderTestName),
                new TemplateToReplace(ShaderFilePathIdentifier, functionToTest.FullPathToFileContainingFunction.SubstringAfter("Assets")),
                new TemplateToReplace(InputArgumentsIdentifier, functionToTest.FunctionArguments.ToInputArgumentsString()),
            };
            lines.ReplaceTemplates(replacements);
            
            IGPUFunctionArguments functionArguments = functionToTest.FunctionArguments;
            lines.AddToEndOfSection(InputBufferDeclarationSection, functionArguments.ToInputBufferDeclarationsString());
            lines.AddToEndOfSection(InOutVariableDeclarationSection, functionArguments.ToInOutVariableDeclarationsString());
            lines.AddToEndOfSection(InOutVariableCollectionSection, functionArguments.ToAssignmentOfInOutVariablesString());
            lines.AddToEndOfSection(SaveDataSection, functionToTest.GetSaveData());
            return String.Join(Environment.NewLine, lines);
        }
        
        #region Template Sections
        private static readonly Section SaveDataSection = new Section("BEGIN SAVE DATA SECTION", "END SAVE DATA SECTION");
        private static readonly Section InputBufferDeclarationSection = new Section("BEGIN INPUT SECTION", "END INPUT SECTION");
        private static readonly Section OutputBufferDeclarationSection = new Section("BEGIN OUTPUT SECTION", "END OUTPUT SECTION");
        private static readonly Section InOutVariableDeclarationSection = new Section("BEGIN DECLARE IN/OUT VARIABLES", "END DECLARE IN/OUT VARIABLES");
        private static readonly Section InOutVariableCollectionSection = new Section("BEGIN COLLECT IN/OUT VARIABLES", "END COLLECT IN/OUT VARIABLES");
        #endregion Template Sections

        #region Template Identifiers
        private const string OutputTypeIdentifier = "OUTPUT_TYPE";
        public const string OutputBufferVariableName = "output";
        private const string FunctionToDebugIdentifier = "FUNCTION_TO_DEBUG";
        private const string ShaderFilePathIdentifier = "FULL_PATH_TO_FILE_CONTAING_FUNCTION_TEMPLATE.cginc";
        private const string InputArgumentsIdentifier = "INPUT_ARGUMENTS";
        #endregion Template Identifiers
        
        #region Generated Text
        public const string InputBufferVariableName = "Input";
        public const string KernelPrefix = "RUN_";
        private const string OutVariablePrefix = "out";
        private const string InOutVariablePrefix = "inOut";
        #endregion Generated Text

        private static readonly string Template = 
$@"#include ""{ShaderFilePathIdentifier}""
#pragma kernel {KernelPrefix}{FunctionToDebugIdentifier}

/* {InputBufferDeclarationSection.SectionOpen} */
/* {InputBufferDeclarationSection.SectionClose} */

/* {OutputBufferDeclarationSection.SectionOpen} */
RWStructuredBuffer<{OutputTypeIdentifier}> {OutputBufferVariableName};
/* {OutputBufferDeclarationSection.SectionClose} */

[numthreads(1,1,1)]
void {KernelPrefix}{FunctionToDebugIdentifier} ()
{{
    /* {InOutVariableDeclarationSection.SectionOpen} */
    /* {InOutVariableDeclarationSection.SectionClose} */

    {OutputBufferVariableName}[0] = {FunctionToDebugIdentifier}({InputArgumentsIdentifier});

    /* {InOutVariableCollectionSection.SectionOpen} */
    /* {InOutVariableCollectionSection.SectionClose} */
}}
/* {SaveDataSection} */";

        private static readonly string[] TemplateLinesArray = Template.Split(new[]
                                                                             {
                                                                                 Environment.NewLine
                                                                             },
                                                                             StringSplitOptions.RemoveEmptyEntries);
        private static List<string> TemplateLinesForEditing => TemplateLinesArray.ToList();

        private static string ToLocalReadWriteVariable(this IGPUFunctionArgument argument, int inputIndex)
        {
            Assert.IsTrue(argument.RequiresWriting);
            string prefix = argument.InputModifier == InputModifier.Out ? OutVariablePrefix : InOutVariablePrefix;
            return $"{prefix}{InputBufferVariableName}{inputIndex}";
        }
        
        private static string ToInputArgumentsString(this IGPUFunctionArguments arguments)
        {
            IGPUFunctionArgument[] args = arguments.GetArguments();
            string[] inputArguments = new string[arguments.ArgumentCount];
            for (var index = 0; index < inputArguments.Length; index++)
            {
                inputArguments[index] = args[index].RequiresWriting
                    ? args[index].ToLocalReadWriteVariable(index)
                    : $"{InputBufferVariableName}{index}[0]";
            }
            return String.Join(", ", inputArguments);
        }
        
        private static string[] ToInputBufferDeclarationsString(this IGPUFunctionArguments arguments)
        {
            string GetDeclaration(IGPUFunctionArgument input, int index)
            {
                string variableName = $"{InputBufferVariableName}{index}";
                return input.RequiresWriting
                    ? GetReadWriteBufferDeclaration(input.Type, variableName)
                    : GetReadOnlyBufferDeclaration(input.Type, variableName);
            }
            return arguments.GetArguments().Select(GetDeclaration).ToArray();
        }
        
        private static string[] ToInOutVariableDeclarationsString(this IGPUFunctionArguments arguments)
        {
            string GetDeclaration(IGPUFunctionArgument input, int index)
            {
                return input.RequiresWriting
                    ? $"{input.Type.GetShaderTypeName()} {input.ToLocalReadWriteVariable(index)} = {InputBufferVariableName}{index}[0];"
                    : null;
            }
            return arguments.GetArguments().Select(GetDeclaration).Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }
        
        private static string[] ToAssignmentOfInOutVariablesString(this IGPUFunctionArguments arguments)
        {
            string GetAssignment(IGPUFunctionArgument input, int index)
            {
                return input.RequiresWriting
                    ? $"{InputBufferVariableName}{index}[0] = {input.ToLocalReadWriteVariable(index)};"
                    : null;
            }
            return arguments.GetArguments().Select(GetAssignment).Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }
        
        private static string GetReadWriteBufferDeclaration(Type type, string variableName)
        {
            return $"RWStructuredBuffer<{type.GetShaderTypeName()}> {variableName};";
        }
        
        private static string GetReadOnlyBufferDeclaration(Type type, string variableName)
        {
            return $"StructuredBuffer<{type.GetShaderTypeName()}> {variableName};";
        }
    }
}