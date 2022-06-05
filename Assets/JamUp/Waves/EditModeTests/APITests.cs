using System;
using System.Collections.Generic;
using System.Dynamic;
using JamUp.JavascriptRunner.Scripts;
using NUnit.Framework;
using UnityEngine;

namespace JamUp.Waves.EditModeTests
{
    public class APITests
    {
        void TestInit(object any)
        {
            KeyFrame frame = Converter.ToKeyFrame(any as ExpandoObject);
        }

        [Test]
        public void Declaration()
        {
            Debug.Log(Generator.APIDeclaration());
            
            /*
             *
export type KeyFrame = {
    sampleRate: number;
    time: number;
    thickness: number;
    duration: number;
    waves: WaveState[];
};
export type WaveState = {
    frequency: number;
    amplitude: number;
    waveType: WaveType;
    phaseDegrees: number;
    displacementAxis: SimpleFloat3;
};
export enum WaveType {
    Sine = 'Sine',
    Square = 'Square',
    Triangle = 'Triangle',
    Sawtooth = 'Sawtooth',
}
export type SimpleFloat3 = {
    x: number;
    y: number;
    z: number;
};
             */
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
    waves: [{{frequency: {3.5}, waveType: 'Sine'}}, {{amplitude: {5.0}, waveType: 'Triangle'}}],
}};

init(obj);
";
           Runner.ExecuteString(code,
                                context =>
                                {
                                    context.AddFunction<Action<object>>("init", TestInit);
                                }); 
        }
    }
}