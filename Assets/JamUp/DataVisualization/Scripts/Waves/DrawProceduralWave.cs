using System;
using System.Collections.Generic;
using System.Linq;
using JamUp.UnityUtility;
using JamUp.Waves;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace JamUp.DataVisualization.Waves
{
    public class DrawProceduralWave : MonoBehaviour
    {
        private List<WaveDrawingState> states;
        private int currentIndex;
        private int numberOfWaves;
        private float lastSwitchTime;

        [SerializeField]
        private Material material;
        
        MaterialPropertyBlock propertyBlock;

        private ShaderProperty<int> WaveCount;
        private ShaderProperty<int> SampleRate;
        private ShaderProperty<Vector4[]> WaveData;
        // (keyStartTime, keyEndTime, keyEndTime - keyStartTime);
        private ShaderProperty<float4> KeyTime;
        private ShaderProperty<float> PropogationScaleProperty;
        private ShaderProperty<float> DisplacementScaleProperty;
        private ShaderProperty<Matrix4x4> WaveOriginToWorldMatrix;
        private ShaderProperty<Matrix4x4>  WorldToWaveOriginMatrix;
        private ShaderProperty<Vector4[]> DisplacementAxes;
        private ShaderProperty<float> Thickness;

        private void Start()
        {
            propertyBlock = new MaterialPropertyBlock();
            WaveCount = new ShaderProperty<int>(nameof(WaveCount));
            SampleRate = new ShaderProperty<int>(nameof(SampleRate));
            WaveData = new ShaderProperty<Vector4[]>(nameof(WaveData));
            KeyTime = new ShaderProperty<float4>(nameof(KeyTime));
            DisplacementAxes = new ShaderProperty<Vector4[]>(nameof(DisplacementAxes));
            Thickness = new ShaderProperty<float>(nameof(Thickness));
            WaveOriginToWorldMatrix = new ShaderProperty<Matrix4x4>(nameof(WaveOriginToWorldMatrix));
            WorldToWaveOriginMatrix = new ShaderProperty<Matrix4x4>(nameof(WorldToWaveOriginMatrix));

            states = GetComponentsInChildren<WaveState>().ToList().Select(behaviour => behaviour.State).ToList();
            Assert.AreNotEqual(states.Count, 0);
            Assert.AreNotEqual(states.Count, 1);
            numberOfWaves = states[0].serializableWaves.Length;
            states.ForEach(state => Assert.AreEqual(state.serializableWaves.Length, numberOfWaves));
            
            lastSwitchTime = Time.timeSinceLevelLoad;
            SetDynamicProperties(states[currentIndex], states[currentIndex + 1]);
        }

        private void Update()
        {
            if (currentIndex == states.Count - 1) return;
            
            var initial = states[currentIndex];
            var target = states[currentIndex + 1];

            if (Time.timeSinceLevelLoad - lastSwitchTime >= initial.duration)
            {
                currentIndex++;
                if (currentIndex == states.Count - 1) return;
                
                lastSwitchTime = Time.timeSinceLevelLoad;
                initial = target;
                SetDynamicProperties(initial, states[currentIndex + 1]);
            }
            
            Bounds bounds = new Bounds(transform.position, Vector3.one * 50f);
            int numberOfVertices = 24 * (int)(initial.time / (1f / initial.sampleRate));
            Graphics.DrawProcedural(material, bounds, MeshTopology.Triangles, numberOfVertices, 0, null, propertyBlock, ShadowCastingMode.TwoSided);
        }

        private void SetDynamicProperties(WaveDrawingState initial, WaveDrawingState target)
        {
            Vector4[] waveData = new Vector4[numberOfWaves * 2];
            Vector4[] displacementAxes = new Vector4[numberOfWaves * 2];
            
            for (var waveIndex = 0; waveIndex < numberOfWaves; waveIndex++)
            {
                int dataIndex = 2 * waveIndex;
                Wave start = initial.serializableWaves[waveIndex].Wave;
                Wave end = target.serializableWaves[waveIndex].Wave;

                waveData[dataIndex] = new Vector4(start.Frequency, start.Amplitude, start.PhaseOffset, (float)start.WaveType);
                displacementAxes[dataIndex] = new float4(initial.serializableWaves[waveIndex].DisplacementAxis, 0f);
                waveData[dataIndex + 1] = new Vector4(end.Frequency, end.Amplitude, end.PhaseOffset, (float)end.WaveType);
                displacementAxes[dataIndex + 1] = new float4(target.serializableWaves[waveIndex].DisplacementAxis, 0f);
            }
            
            propertyBlock.SetProperty(KeyTime, new float4(Time.timeSinceLevelLoad,
                                                          Time.timeSinceLevelLoad + initial.duration,
                                                          initial.duration,
                                                          0f));
            
            propertyBlock.SetProperty(SampleRate, initial.sampleRate);
            propertyBlock.SetProperty(WaveCount, numberOfWaves);
            propertyBlock.SetProperty(WaveData, waveData);
            propertyBlock.SetProperty(DisplacementAxes, displacementAxes);
            propertyBlock.SetProperty(Thickness, initial.thickness);
            propertyBlock.SetProperty(PropogationScaleProperty, 1.0f);
            propertyBlock.SetProperty(DisplacementScaleProperty, 1.0f);
            propertyBlock.SetProperty(WaveOriginToWorldMatrix, transform.localToWorldMatrix);
            propertyBlock.SetProperty(WorldToWaveOriginMatrix, transform.worldToLocalMatrix);
        }
    }
}