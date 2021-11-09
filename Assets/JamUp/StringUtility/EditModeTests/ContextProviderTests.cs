using System.CodeDom;
using JamUp.TestUtility;
using NUnit.Framework;
using UnityEngine;
using static JamUp.StringUtility.ContextProvider;

namespace JamUp.StringUtility.EditModeTests
{
    public class ContextProviderTests : TestBase
    {
        private struct DummyStruct
        {
            public DummyStruct(object dummy)
            {
                Debug.Log(Context());

            }
        }
        
        [Test]
        public void Test()
        {
            Debug.Log(Context());
            new DummyStruct(default);
        }

        public override void Setup()
        {
        }

        public override void TearDown()
        {
        }
    }
}