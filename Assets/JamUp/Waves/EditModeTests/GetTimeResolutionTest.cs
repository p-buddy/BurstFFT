using NUnit.Framework;
using Assert = UnityEngine.Assertions.Assert;

using pbuddy.ShaderUtility.EditorScripts;

namespace JamUp.Waves.EditModeTests
{
    public class GetTimeResolutionTest
    {
        [Test]
        public void Test()
        {
            var arguments = new UnnamedGPUFunctionArguments(GPUFunctionArgument.In((uint)100));
            arguments.SendToCgFunctionAndGetOutput("WaveFunctions", "GetTimeResolution", out float time);
            Assert.AreEqual(0.01f, time);
        }
    }
}