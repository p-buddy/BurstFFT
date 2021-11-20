using System;
using JamUp.UnityUtility.Editor;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.UnityUtility.EditModeTests
{
    public class DebugShaderFunctionTests
    {
        [Test]
        public void TestFunctionWithInOutVariable()
        {
            string gpuFunctionToTest = @"
float3 SomeFunction(inout int i)
{
    int before = i;
    i = -i;
    return float3(before, before, before);
}
";
            DebugAndTestGPUCodeUtility.GenerateCgIncFile(gpuFunctionToTest,
                                                         out string fileName,
                                                         out Action onFinishedWithFile);

            int value = 3;
            var input = new NamedGPUFunctionArguments
            {
                Argument0 = GPUFunctionArgument.InOut(value)
            };
            
            input.SendToGPUFunctionAndGetOutput(fileName, "SomeFunction", out float3 x);
            Assert.AreEqual(x, new float3(value, value, value));
            Assert.AreEqual(input.Argument0.GetValue<int>(), -value);
            
            onFinishedWithFile.Invoke();
        }
    }
}