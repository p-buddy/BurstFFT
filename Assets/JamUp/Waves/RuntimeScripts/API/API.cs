using System;
using UnityEngine;
using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Entities;

namespace JamUp.Waves.RuntimeScripts.API
{
    public class API: APIBase<API.Domain>
    {
        public override IClrToTsNameMapper NameMapper => ClrToTsNameMapper.PascalToCamelCase;
        
        private CreateSignalEntitiesSystem entitiesSystem;

        private CreateSignalEntitiesSystem CreateEntitiesSystem
        {
            get
            {
                entitiesSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<CreateSignalEntitiesSystem>();
                return entitiesSystem;
            }
        }
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
            CreateEntitiesSystem.EnqueueSignal(in signal);
        }
    }
}