using System;
using System.Collections.Generic;
using System.Dynamic;
using JamUp.Waves.Scripts.API;
using NUnit.Framework;
using pbuddy.TypeScriptingUtility.RuntimeScripts;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace JamUp.Waves.EditModeTests
{
    public class APITests
    {
        void TestInit(object any)
        {
            KeyFrame frame = JaToClrConverter.To<KeyFrame>(any as ExpandoObject);
            
            Assert.AreEqual(frame.SampleRate, 100);
            Assert.AreEqual(frame.Time, 100.5f);
            Assert.AreEqual(frame.Thickness, 100.5f);
            
        }

        [Test]
        public void LogAPI()
        {
            Debug.Log(APIGeneration.GenerateDeclarations());
        }
        
        [Test]
        public void Test()
        {
            var code = $@"
let obj = {{
    sampleRate: {100},
    time: {100.5},
    thickness: {1.4},
    duration: {10.5},
    waves: [{{frequency: {3.5}, waveType: 'Sine', displacementAxis: {{x: 1, y: 2, z: 3}}}}, {{amplitude: {5.0}, waveType: 'Triangle', displacementAxis: {{x: 4, y: 5, z: 6}}}}],
}};

{APIGeneration.InternalInitFunc}(obj);
";
            
           JsRunner.ExecuteString(code,
                                context =>
                                {
                                    context.AddFunction<Action<object>>(APIGeneration.InternalInitFunc, TestInit);
                                }); 
        }
    }
}