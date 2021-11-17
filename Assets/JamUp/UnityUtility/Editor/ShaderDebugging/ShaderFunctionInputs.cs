using System;
using System.Linq;
using System.Reflection;

namespace JamUp.UnityUtility.Editor
{
    public interface IShaderFunctionInput
    {
        Type[] GetInputTypes();
        object[] GetInputValues();
    }

    public abstract class AbstractShaderFunctionInputs : IShaderFunctionInput
    {
        protected Type[] genericTypes;
        public Type[] GetInputTypes()
        {
            genericTypes ??= GetType().GetGenericArguments();
            return genericTypes;
        }

        public object[] GetInputValues()
        {
            FieldInfo[] members = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            return members.ToList().OrderBy(info => info.Name).Select(member => member.GetValue(this)).ToArray();
        }
    }
    
    public class ShaderFunctionInputs<T0> : AbstractShaderFunctionInputs
    {
        public T0 Input0;
    }
    
    public class ShaderFunctionInputs<T0, T1> : ShaderFunctionInputs<T0>
    {
        public T1 Input1;
    }
    
    public class ShaderFunctionInputs<T0, T1, T2> : ShaderFunctionInputs<T0, T1>
    {
        public T2 Input2;
    }
    
    public class ShaderFunctionInputs<T0, T1, T2, T3> : ShaderFunctionInputs<T0, T1, T2>
    {
        public T3 Input3;
    }
    
    public class ShaderFunctionInputs<T0, T1, T2, T3, T4> : ShaderFunctionInputs<T0, T1, T2, T3> 
    {
        public T4 Input4;
    }
    
    public class ShaderFunctionInputs<T0, T1, T2, T3, T4, T5> : ShaderFunctionInputs<T0, T1, T2, T3, T4>
    {
        public T5 Input5;
    }
}