using System.Collections.Generic;
using JamUp.Waves.RuntimeScripts;
using NUnit.Framework;
using Unity.Mathematics;
using Assert = UnityEngine.Assertions.Assert;

using pbuddy.ShaderUtility.EditorScripts;
using pbuddy.StringUtility.RuntimeScripts;

namespace JamUp.Waves.EditModeTests
{
    public class GetDisplacementAtTimeTests
    {
        [SetUp]
        public void Setup()
        {
            SupportedShaderTypes.AddSupportForType<ShaderWave>("Wave");
        }
        
        public struct TestCase
        {
            public ShaderWave Wave;
            public float Time;
            public float3 DisplacementVector;
            public float3 ExpectedOutput;
            
            public float3 RunFunctionAndGetOutput()
            {
                var arguments = new NonspecificNamedGPUFunctionArguments
                {
                    Argument0 = GPUFunctionArgument.In(Wave),
                    Argument1 = GPUFunctionArgument.In(Time),
                    Argument2 = GPUFunctionArgument.In(DisplacementVector)
                };
                arguments.SendToCgFunctionAndGetOutput("WaveFunctions", "GetDisplacementAtTime", out float3 output);
                return output;
            }

            public void AssertOutputMatchesExpect(float3 output)
            {
                Assert.AreEqual(ExpectedOutput, output);
            }

            public override string ToString() => this.NameAndPublicData(true);
        }
        
        public static IEnumerable<TestCase> GetTestCases => new[]
        {
            new TestCase
            {
                Wave = ShaderWave.FromManagedWave(new Wave(WaveType.Sine, 1f)),
                Time = 0f,
                DisplacementVector = math.forward(),
                ExpectedOutput = float3.zero,
            },
            new TestCase
            {
                Wave = ShaderWave.FromManagedWave(new Wave(WaveType.Sine, 1f, 0f, 0.5f)),
                Time = 0.25f,
                DisplacementVector = math.up(),
                ExpectedOutput = math.up() * 0.5f,

            },
            new TestCase
            {
                Wave = ShaderWave.FromManagedWave(new Wave(WaveType.Sine, 1f)),
                Time = 0.5f,
                DisplacementVector = math.forward()
            },
            new TestCase
            {
                Wave = ShaderWave.FromManagedWave(new Wave(WaveType.Sine, 1f, 0f, 2f)),
                Time = 0.75f,
                DisplacementVector = math.right(),
                ExpectedOutput = -math.right() * 2f
            },
            new TestCase
            {
                Wave = ShaderWave.FromManagedWave(new Wave(WaveType.Sine, 1f)),
                Time = 1f,
                DisplacementVector = math.forward()
            }
        };

        [Test]
        public void Test([ValueSource(nameof(GetTestCases))] TestCase testCase)
        {
            float3 value = testCase.RunFunctionAndGetOutput();
            testCase.AssertOutputMatchesExpect(value);
        }
    }
}