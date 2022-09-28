using System;
using UnityEngine;
using pbuddy.TypeScriptingUtility.RuntimeScripts;

namespace JamUp.Waves.Scripts.API
{
    public class API: APIBase<API.Domain>
    {
        public new struct Domain
        {
            public Shared<Type> TimeLineClass;
            public Shared<Action<Signal, float>> PlayBack;
        }

        protected override Domain Define() => new()
        {
            TimeLineClass = TsType.Class<Signal>(nameof(Signal)),
            PlayBack = TsType.Function<Action<Signal, float>>("play", PlayBack)
        };
        

        private void PlayBack(Signal signal, float startTime)
        {
            // const waves = [];
            // const start = {};
            // const signal1 = new Signal(start);
            // signal1.AddFrame({ 
            //  Projection: { value: ProjectionType.Orthographic, animation: AnimationCurve.EaseBegin }
            //  SampleRate: 12, 
            //  Waves: [],
            // },
            // {
            //  Projection: AnimationCurve.EaseIn,
            //  
            // })
            // play(signal1, 0);
        }
    }
}