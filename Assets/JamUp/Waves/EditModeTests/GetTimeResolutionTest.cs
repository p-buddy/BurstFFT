using JamUp.ShaderUtility.Editor;
using NUnit.Framework;
using Assert = UnityEngine.Assertions.Assert;

namespace JamUp.Waves.EditModeTests
{
    public class GetTimeResolutionTest
    {
        [Test]
        public void Test()
        {
            var arguments = new NamelessGPUFunctionArguments(GPUFunctionArgument.In((uint)100));
            arguments.SendToCgFunctionAndGetOutput("WaveFunctions", "GetTimeResolution", out float time);
            Assert.AreEqual(0.01f, time);
        }
    }
}