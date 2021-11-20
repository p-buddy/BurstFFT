using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace JamUp.UnityUtility.Editor
{
    [Serializable]
    public class TestableGPUFunction
    {
        #region Serialized Info (For Saving)
        public string FunctionUnderTest;
        public string FullPathToFileContainingFunction;
        public string ReturnTypeName;
        public string[] ArgumentTypeNames;
        public string[] InputModifierNames;
        #endregion Serialized Info (For Saving)

        #region Private properties for converting strings to usable types
        private Type[] InputTypeNamesToTypes => ArgumentTypeNames.Select(SupportedShaderTypes.LookUpManagedType).ToArray();
        private InputModifier[] InputModifierStringsToValues => InputModifierNames.Select(InputModiferValueForName).ToArray();
        #endregion  Private properties for converting strings to usable types

        #region RunTime Info
        public Type OutputType { get; private set; }
        public IGPUFunctionArguments FunctionArguments { get; private set; }
        #endregion RunTime Info
        public TestableGPUFunction(string functionUnderTest,
                                   string fullPathToFileContainingFunction,
                                   Type outputType,
                                   IGPUFunctionArguments functionArguments)
        {
            FunctionUnderTest = functionUnderTest;
            FullPathToFileContainingFunction = fullPathToFileContainingFunction;
            OutputType = outputType;
            FunctionArguments = functionArguments;
            ReturnTypeName = outputType.Name;
            ArgumentTypeNames = functionArguments.GetInputTypes().Select(type => type.Name).ToArray();
            InputModifierNames = functionArguments.GetModifiers().Select(InputModiferNameForValue).ToArray();
        }

        public void SetFunctionArgumentValues(object[] values)
        {
        }

        public string GetSaveData()
        {
            return JsonUtility.ToJson(this, true);
        }

        public bool IsCompatible(TestableGPUFunction other)
        {
            return FunctionUnderTest == other.FunctionUnderTest &&
                   FullPathToFileContainingFunction == other.FullPathToFileContainingFunction &&
                   FunctionArguments.ArgumentCount == other.FunctionArguments.ArgumentCount &&
                   other.OutputType == OutputType &&
                   FunctionArguments.GetInputTypes().SequenceEqual(other.FunctionArguments.GetInputTypes()) &&
                   FunctionArguments.GetArguments().SequenceEqual(other.FunctionArguments.GetArguments());
        }

        public static TestableGPUFunction FromSaveData(string saveData)
        {
            object GetDefaultValue(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;
             
            var gpuFunction = JsonUtility.FromJson<TestableGPUFunction>(saveData);
            gpuFunction.OutputType = SupportedShaderTypes.SupportedManagedTypeByTypeName[gpuFunction.ReturnTypeName];

            Type[] inputTypes = gpuFunction.InputTypeNamesToTypes;
            InputModifier[] inputModifiers = gpuFunction.InputModifierStringsToValues;
            Assert.AreEqual(inputTypes.Length, inputModifiers.Length);
            
            IGPUFunctionArgument GetFunctionArgument(int index)
            {
                Type type = inputTypes[index];
                return new GPUFunctionArgument(type, inputModifiers[index], GetDefaultValue(type));
            }

            IGPUFunctionArgument[] functionArguments = Enumerable.Range(0, inputTypes.Length)
                                                                 .Select(GetFunctionArgument)
                                                                 .ToArray();
            gpuFunction.FunctionArguments = new NamelessGPUFunctionArguments(functionArguments);
            return gpuFunction;
        }

        private static string InputModiferNameForValue(InputModifier modifier) 
            => Enum.GetNames(typeof(InputModifier))[(int)modifier];

        private static InputModifier InputModiferValueForName(string name) =>
            (InputModifier)Enum.Parse(typeof(InputModifier), name);
    }
}