using System.Collections.Generic;
using JamUp.ShaderUtility.Editor;
using JamUp.StringUtility;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace JamUp.Waves.EditModeTests
{
    public class GetCoordinateAxesTest
    {
        [SetUp]
        public void Setup()
        {
            SupportedShaderTypes.AddSupportForType<CoordinateAxes>();
        }

        public struct TestCase
        {
            public float3 Forward;
            public float3 ExpectedRight;
            public float3 ExpectedUp;
            public float3 ExpectedForward;
            public CoordinateAxes RunFunctionAndGetOutput()
            {
                var arguments = new NamedGPUFunctionArguments
                {
                    Argument0 = GPUFunctionArgument.In(Forward)
                };
                arguments.SendToCgFunctionAndGetOutput("WaveFunctions", "GetCoordinateAxes", out CoordinateAxes output);
                return output;
            }

            public override string ToString()
            {
                return this.NameAndPublicData(true);
            }

            public void AssertExpectedMatchesActual(CoordinateAxes axes)
            {
                Assert.AreEqual(ExpectedRight, axes.Right, $"{nameof(ExpectedRight)}: {ExpectedRight} vs {axes.Right}");
                Assert.AreEqual(ExpectedUp, axes.Up, $"{nameof(ExpectedUp)}: {ExpectedUp} vs {axes.Up}");
                Assert.AreEqual(ExpectedForward, axes.Forward, $"{nameof(ExpectedForward)}: {ExpectedForward} vs {axes.Forward}");
            }
        }

        public static IEnumerable<TestCase> GetTestCases()
        {
            return new[]
            {
                new TestCase
                {
                    Forward = math.forward(),
                    ExpectedRight = math.right(),
                    ExpectedUp = math.up(),
                    ExpectedForward = math.forward(),
                },
                new TestCase
                {
                    Forward = math.right(),
                    ExpectedRight = -math.forward(),
                    ExpectedUp = math.up(),
                    ExpectedForward = math.right(),
                },
                new TestCase
                {
                    Forward = -math.up(),
                    ExpectedRight = math.right(),
                    ExpectedUp = math.forward(),
                    ExpectedForward = -math.up(),
                },
                new TestCase
                {
                    Forward = -math.right(),
                    ExpectedRight = math.forward(),
                    ExpectedUp = math.up(),
                    ExpectedForward = -math.right(),
                },
            };
        }

        [Test]
        public void Test([ValueSource(nameof(GetTestCases))] TestCase testCase)
        {
            CoordinateAxes axes = testCase.RunFunctionAndGetOutput();
            testCase.AssertExpectedMatchesActual(axes);
        }
    }
}