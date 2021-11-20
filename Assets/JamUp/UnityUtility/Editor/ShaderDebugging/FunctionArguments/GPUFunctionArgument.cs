using System;

using UnityEngine.Assertions;

using static JamUp.StringUtility.ContextProvider;

namespace JamUp.UnityUtility.Editor
{
    public struct GPUFunctionArgument : IGPUFunctionArgument
    {
        public Type Type { get; }
        public InputModifier InputModifier { get; }
        public object Value { get; private set; }
        public bool RequiresWriting => InputModifier == InputModifier.Out || InputModifier == InputModifier.InOut;

        public void SetValue(object updatedValue)
        {
            Assert.IsTrue(updatedValue.GetType() == Type);
            Value = updatedValue;
        }

        internal GPUFunctionArgument(Type type, InputModifier inputModifier, object value)
        {
            Assert.IsTrue(type.IsConvertibleToShaderType(), $"{Context()}{type} type cannot be converted to a type usable in GPU code.");
            Type = type;
            InputModifier = inputModifier;
            Value = value;
        }

        public static GPUFunctionArgument In<T>(T value) where T : struct
        {
            return new GPUFunctionArgument(typeof(T), InputModifier.In, value);
        }
        
        public static GPUFunctionArgument InOut<T>(T value) where T : struct
        {
            return new GPUFunctionArgument(typeof(T), InputModifier.InOut, value);

        }
        
        public static GPUFunctionArgument Out<T>() where T : struct
        {
            return new GPUFunctionArgument(typeof(T), InputModifier.Out, default);
        }
        
        public T GetValue<T>() where T : struct
        {
            Assert.IsTrue(typeof(T) == Type);
            return (T)Value;
        }
    }
}