using System.Collections;

using JamUp.Waves;
using JamUp.Waves.Scripts;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

using pbuddy.TestUtility.EditorScripts;

namespace JamUp.DataVisualization.PlayModeTests
{
    public class DrawAsMeshTests : TestBase
    {
        [UnityTest]
        public IEnumerator DrawAxes()
        {
            CameraAccess.SetCamera(math.up() - math.right() - math.forward(), float3.zero);
            DrawAsMesh.Axes3D(10f, Vector3.zero, 0.1f);
            yield return new WaitForSeconds(40f);
        }
        
        [UnityTest]
        public IEnumerator DrawWave()
        {
            Wave wave = new Wave(WaveType.Sine, 1f);
            Wave cosWave = Wave.SinToCos(in wave);

            CameraAccess.SetCamera(math.up() - math.right() - math.forward(), float3.zero);
            //DrawAsMesh.RealWave(in wave, 1f, 44100, Color.cyan, 0.1f);
            //DrawAsMesh.RealWave(in cosWave, 1f, 44100, Color.yellow, 0.1f);
            DrawAsMesh.RealWave(in wave, 1f, 44100, Color.green, 0.1f);
            yield return new WaitForSeconds(120f * 10);
        }
        
        [UnityTest]
        public IEnumerator DrawLine()
        {
            CameraAccess.SetCamera(math.up() - math.right() - math.forward(), float3.zero);
            DrawAsMesh.Line(new []{Vector3.up, Vector3.down, Vector3.down * 2f, Vector3.right * 5f, Vector3.up * 2f}, 1f, Color.blue);
            yield return new WaitForSeconds(30f);
        }

        public override void Setup()
        {
        }

        public override void TearDown()
        {
        }
    }
}
