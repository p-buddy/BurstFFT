using System;

namespace JamUp.UnityUtility.Editor
{
    public interface IGPUFunctionArguments
    {
        int ArgumentCount { get; }
        Type[] GetInputTypes();
        object[] GetInputValues();
        InputModifier[] GetModifiers();
        IGPUFunctionArgument[] GetArguments();
        void SetValues(object[] updatedValues);
        void UpdateInputValueAtIndex(object updatedValue, int index);
    }
}