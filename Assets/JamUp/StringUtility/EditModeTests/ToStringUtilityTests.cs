using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

using JamUp.TestUtility;

namespace JamUp.StringUtility.EditModeTests
{
    public class ToStringUtilityTests : TestBase
    {
        public enum DummyEnum { Item1, Item2, Item3 }
        public struct DummyObjectWithPrimitives { public int DummyField; public bool DummyProperty { get; } public DummyEnum DummyEnum; }
        public struct DummyObjectWithNestedType { public int DummyField; public DummyObjectWithPrimitives DummyNestedType { get; } }
        
        public struct DummyObjectWithManyNestedTypes
        {
            public struct Level1
            {
                public Level1(object dummy = default)
                {
                    LevelDown = new Level2(dummy);
                }
                
                public Level2 LevelDown { get; }
                
                public struct Level2
                {
                    public class Level3
                    {
                        public float[] Array = { default, default };
                    }

                    public Level3 UnSetLevel;
                    public Level3 SetLevel;

                    public Level2(object dummy = default)
                    {
                        UnSetLevel = default;
                        SetLevel = new Level3();
                    }
                }

            }

            public Level1 LevelDown;
            public DummyObjectWithPrimitives DummySingleLevelNestedType { get; }
            
            public DummyObjectWithManyNestedTypes(object dummy)
            {
                LevelDown = new Level1(dummy);
                DummySingleLevelNestedType = new DummyObjectWithPrimitives();
            }
        }

        public override void Setup()
        {
        }

        public override void TearDown()
        {
            ClearConsoleLogs();
        }

        private static List<string> TestCases = new List<string>()
        {
            ToStringHelper.NameAndPublicData(1, true),
            ToStringHelper.NameAndPublicData(1, false),
            ToStringHelper.NameAndPublicData(new DummyObjectWithPrimitives(), true),
            ToStringHelper.NameAndPublicData(new DummyObjectWithPrimitives(), false),
            ToStringHelper.NameAndPublicData(new DummyObjectWithNestedType(), true),
            ToStringHelper.NameAndPublicData(new DummyObjectWithNestedType(), false),
            ToStringHelper.NameAndPublicData(new DummyObjectWithManyNestedTypes(default), true),
            ToStringHelper.NameAndPublicData(new DummyObjectWithManyNestedTypes(default), false),
        };
        
        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void Log(string readout)
        {
            ToStringHelper.NameAndPublicData(new DummyObjectWithManyNestedTypes(), true);
            Debug.Log(readout);
        }
    }
}
