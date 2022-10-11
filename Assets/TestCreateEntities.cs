using System.Collections;
using System.Collections.Generic;
using JamUp.Waves.Scripts;
using JamUp.Waves.Scripts.API;
using Unity.Entities;
using UnityEngine;

public class TestCreateEntities : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Signal signal = new Signal(100f);
        signal.AddFrame(new KeyFrame(10f, 100, ProjectionType.Perspective, 1.0f, new []
        {
            new WaveState(1f, 1f, 0f, WaveType.Sine, new SimpleFloat3(0f, 1f, 0f))
        }, 1f));
        CreateSignalEntitiesSystem system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<CreateSignalEntitiesSystem>();
        //system.EnqueueSignal(in signal);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
