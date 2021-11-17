using JamUp.UnityUtility.Editor;
using NUnit.Framework;
using Unity.Mathematics;

namespace JamUp.UnityUtility.EditModeTests
{
    public class DebugShaderFunctionTests
    {
        [Test]
        public void DebugShader()
        {
            uint vertexIndex = 1;
            float3 samplePosition = 0;
            float3 nextSamplePosition = 0;
            float3 sampleTangent = 0;
            float3 nextSampleTangent = 0;
            float thickness = 1f;
            var input = new ShaderFunctionInputs<uint, float3, float3, float3, float3, float>()
            {
                Input0 = vertexIndex,
                Input1 = samplePosition,
                Input2 = nextSamplePosition,
                Input3 = sampleTangent,
                Input4 = nextSampleTangent,
                Input5 = thickness,
            };
            using (new DebugShaderFunction<float3>("", "", input, out float3 x))
            {
                var y = 0;
            }
        }
    }
}