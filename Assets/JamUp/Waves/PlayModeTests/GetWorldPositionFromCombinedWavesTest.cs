using System.Collections;
using System.Linq;
using JamUp.Waves.Scripts;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

using pbuddy.ShaderUtility.EditorScripts;

namespace JamUp.Waves
{
    public class GetWorldPositionFromCombinedWavesTest
    {
        public struct TestCase
        {
            public uint VertexIndex;
            public uint SampleRate;
            public float4x4 WaveOriginToWorldMatrix;
            public int NumberOfWaves;
            public float3[] DisplacementAxes;
            public float[] Frequencies;
            public float[] Amplitudes;
            public float[] Phases;
            public uint[] WaveTypes;
            public float Thickness;

            public float3 RunFunctionAndGetOutput()
            {
                var arguments = new NonspecificNamedGPUFunctionArguments()
                {
                    Argument0 = GPUFunctionArgument.In(VertexIndex),
                    Argument1 = GPUFunctionArgument.In(SampleRate),
                    Argument2 = GPUFunctionArgument.In(WaveOriginToWorldMatrix),
                    Argument3 = GPUFunctionArgument.In(NumberOfWaves),
                    Argument4 = GPUFunctionArgument.In(DisplacementAxes),
                    Argument5 = GPUFunctionArgument.In(Frequencies),
                    Argument6 = GPUFunctionArgument.In(Amplitudes),
                    Argument7 = GPUFunctionArgument.In(Phases),
                    Argument8 = GPUFunctionArgument.In(WaveTypes),
                    Argument9 = GPUFunctionArgument.In(Thickness)
                };
                arguments.SendToCgFunctionAndGetOutput("WaveFunctions", "GetWorldPositionFromCombinedWaves", out float3 output);
                return output;
            }
        }

        [UnityTest]
        public IEnumerator Test()
        {
            const int numberOfVerts = 24 * 40;
            const uint sampleRate = 20;
            const int numberOfWaves = 2;
            float3[] verts = new float3[numberOfVerts];
            for (uint index = 0; index < numberOfVerts; index++)
            {
                TestCase testCase = new TestCase
                {
                    VertexIndex = index,
                    SampleRate = sampleRate,
                    WaveOriginToWorldMatrix = float4x4.identity,
                    NumberOfWaves = numberOfWaves,
                    DisplacementAxes = Enumerable.Repeat(math.up(), 10).ToArray(),
                    Frequencies = Enumerable.Repeat(1f, 10).ToArray(),
                    Amplitudes = Enumerable.Repeat(1f, 10).ToArray(),
                    Phases = Enumerable.Repeat(0f, 10).ToArray(),
                    WaveTypes = Enumerable.Repeat((uint)WaveType.Sine, 10).ToArray(),
                    Thickness = 0.25f,
                };
                testCase.Phases[1] = math.radians(-90f);
                testCase.DisplacementAxes[1] = math.right();
                verts[index] = testCase.RunFunctionAndGetOutput();
            }

            for (var index = 0; index < verts.Length; index+=3)
            {
                Debug.DrawLine(verts[index], verts[index + 1], Color.blue, 10f);
                Debug.DrawLine(verts[index], verts[index + 2], Color.blue, 10f);
                Debug.DrawLine(verts[index + 1], verts[index + 2], Color.blue, 10f);
            }
            yield return null;
            Debug.Break();
            yield return null;
            yield return null;
            var x = 0;
        }
    }
}