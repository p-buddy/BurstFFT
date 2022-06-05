using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using JamUp.JavascriptRunner.Scripts;

namespace JamUp.JavascriptRunner.EditModeTests
{
    public class RunnerTests
    {
        [Test]
        public void Log()
        {
            string testString = "This is a javascript log!";
            LogAssert.Expect(LogType.Log, testString);
            Runner.ExecuteString($"console.log(\"{testString}\")");
        }
        
        [Test]
        public void Warn()
        {
            string testString = "Be careful with javascript!";
            LogAssert.Expect(LogType.Warning, testString);
            Runner.ExecuteString($"console.warn(\"{testString}\")");
        }
        
        [Test]
        public void Error()
        {
            string testString = "Uh oh!! JS had an error";
            LogAssert.Expect(LogType.Error, testString);
            Runner.ExecuteString($"console.error(\"{testString}\")");
        }
    }
}