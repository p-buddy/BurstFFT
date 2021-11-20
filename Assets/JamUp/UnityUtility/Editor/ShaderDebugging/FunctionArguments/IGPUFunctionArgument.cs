using System;

namespace JamUp.UnityUtility.Editor
{
    public interface IGPUFunctionArgument
    {
        Type Type { get; }
        InputModifier InputModifier { get; }
        object Value { get; }
        bool RequiresWriting { get; }

        void SetValue(object updatedValue);
        public T GetValue<T>() where T : struct;
    }
}