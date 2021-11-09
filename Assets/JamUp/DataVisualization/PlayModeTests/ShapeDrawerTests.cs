using System.Collections;
using JamUp.TestUtility;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

namespace JamUp.DataVisualization.PlayModeTests
{
    public class ShapeDrawerTests : TestBase
    {
        [UnityTest]
        public IEnumerator DrawAxes()
        {
            ShapeDrawer.DoForever(new Axes3D(100f));
            ShapeDrawer.SetCamera(math.up() - math.right() - math.forward(), float3.zero);
            yield return new WaitForSeconds(10f);
            
        }

        public override void Setup()
        {
        }

        public override void TearDown()
        {
            ShapeDrawer.DestroyInstance();
        }
    }
}
