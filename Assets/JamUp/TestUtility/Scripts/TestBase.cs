using System.Reflection;
using NUnit.Framework;

namespace JamUp.TestUtility
{
    public abstract class TestBase
    {
        [SetUp]
        public abstract void Setup();

        [TearDown]
        public abstract void TearDown();
        
        protected static void ClearConsoleLogs()
        {
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method?.Invoke(new object(), null);
        }
    }
}