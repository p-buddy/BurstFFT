using JamUp.ShaderUtility.Editor;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.Waves.EditModeTests
{
    public class GetTangentAtTimeTests
    {
        [Test]
        public void Test()
        {
            var wave = new ShaderWave { Frequency = 1f, Amplitude = 1f, WaveType = (int)WaveType.Sine, PhaseRadians = 0f};
            var arguments = new NamelessGPUFunctionArguments(GPUFunctionArgument.In(wave),
                                                             GPUFunctionArgument.In(0f),
                                                             GPUFunctionArgument.In(0.00001f),
                                                             GPUFunctionArgument.In(math.up()),
                                                             GPUFunctionArgument.In(math.forward()));
            arguments.SendToCgFunctionAndGetOutput("WaveFunctions", "GetTangentAtTime", out float3 output);
            Debug.Log(output);
        }
    }
}