using System.Collections.Generic;
using System.Linq;
using JamUp.ShaderUtility.Editor;
using JamUp.StringUtility;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace JamUp.Waves.EditModeTests
{
    public class TestWaveShaderFunctions
    {
        [SetUp]
        public void Setup()
        {
            SupportedShaderTypes.AddSupportForType<DebugVertexPosition>();
        }
        public struct TestCase
        {
            public uint VertexIndex;
            public float3 SamplePosition;
            public float3 NextSamplePosition;
            public float3 SampleTangent;
            public float3 NextSampleTangent;
            public float Thickness;
            public float3 RunFunctionAndGetOutput()
            {
                var arguments = new NamedGPUFunctionArguments
                {
                    Argument0 = GPUFunctionArgument.In(VertexIndex),
                    Argument1 = GPUFunctionArgument.In(SamplePosition),
                    Argument2 = GPUFunctionArgument.In(NextSamplePosition),
                    Argument3 = GPUFunctionArgument.In(SampleTangent),
                    Argument4 = GPUFunctionArgument.In(NextSampleTangent),
                    Argument5 = GPUFunctionArgument.In(Thickness),
                    // Argument6 = GPUFunctionArgument.Out<DebugVertexPosition>()
                };
                arguments.SendToCgFunctionAndGetOutput("WaveFunctions", "GetVertexPosition", out float3 output);
                // Debug.Log(arguments.Argument6.GetValue<DebugVertexPosition>());
                return output;
            }

            public override string ToString()
            {
                return this.NameAndPublicData(true);
            }
        }
        
        private static TestCase DefaultWithIndex(uint index)
        {
            return new TestCase
            {
                VertexIndex = index,
                SamplePosition = float3.zero,
                NextSamplePosition = math.forward(),
                SampleTangent = math.forward(),
                NextSampleTangent = math.forward(),
                Thickness = 1f,
            };
        }
        
        private static float3 IndexToCorner(uint index)
        {
            float3 right = math.right() * 0.5f;
            float3 up = math.up() * 0.5f;
            float3 forward = math.forward();
            uint mod24 = index % 24;
            switch (mod24)
            {
                case 0: case 10: case 12:
                    // Front face, right top corner (looking down tangent)
                    return up + right;
                case 1: case 3: case 15:
                    // Front face, left top corner (looking down tangent)
                    return up - right;
                case 4: case 6: case 18:
                    // Front face, left bottom corner (looking down tangent)
                    return -up - right;
                case 7: case 9: case 21:
                    // Front face, right bottom corner (looking down tangent)
                    return -up + right;
                case 2: case 13: case 17:
                    // Back face, right top corner (looking down tangent)
                    return forward + up + right;
                case 5: case 16: case 20:
                    // Back face, left top corner (looking down tangent)
                    return forward + up - right;
                case 8: case 19: case 23:
                    // Back face, left bottom corner (looking down tangent)
                    return forward - up - right;
                case 11: case 22: case 14:
                    // Back face, right bottom corner (looking down tangent)
                    return forward - up + right;
                default:
                    return default;
            }
        }

        public static IEnumerable<TestCase> GetDefaultTestCases()
        {
             return Enumerable.Range(0, 100).Select(index => (uint)index).Select(DefaultWithIndex);
        }

        [Test]
        public void GetVertexPositionTest([ValueSource(nameof(GetDefaultTestCases))]TestCase testCase)
        {
            float3 output = testCase.RunFunctionAndGetOutput();
            Assert.AreEqual(output, IndexToCorner(testCase.VertexIndex));
        }
    }
}