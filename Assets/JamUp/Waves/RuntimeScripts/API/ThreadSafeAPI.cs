using System;
using System.Collections.Generic;
using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace JamUp.Waves.RuntimeScripts.API
{
    public class ThreadSafeAPI: APIBase<ThreadSafeAPI.Domain>
    {
        public List<Signal> Signals { get; } = new ();
        public override IClrToTsNameMapper NameMapper { get; }
        
        public ThreadSafeAPI()
        {
            NameMapper = ClrToTsNameMapper.PascalToCamelCase;
        }

        public new struct Domain
        {
            public Shared<Type> TimeLineClass;
            public Shared<Action<Signal, float>> PlayBack;
            public Shared<RNG> Random;
            public Shared<Func<int, RNG>> GetRandom;
        }

        protected override Domain Define() => new()
        {
            TimeLineClass = TsType.Class<Signal>(nameof(Signal)),
            PlayBack = TsType.Function<Action<Signal, float>>("play", Add),
            Random = TsType.Variable("rng", new RNG(0)),
            GetRandom = TsType.Function<Func<int, RNG>>("getRNG", (seed) => new RNG(seed))
        };

        private void Add(Signal signal, float time) => Signals.Add(signal);

        public void Reset() => Signals.Clear();
    }
}