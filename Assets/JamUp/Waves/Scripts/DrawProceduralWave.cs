using System;
using System.Collections.Generic;
using JamUp.UnityUtility;
using JamUp.Waves.Scripts.API;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace JamUp.Waves.Scripts
{
    public class DrawProceduralWave : MonoBehaviour
    {
        private List<KeyFrame> states;
        private int currentIndex;
        private int numberOfWaves;
        private float lastSetTime;

        [SerializeField]
        private Material material;
        
        MaterialPropertyBlock propertyBlock;

        private ShaderProperty<int> WaveCount;
        private ShaderProperty<int> SampleRate;
        private ShaderProperty<Vector4[]> WaveData;
        // (keyStartTime, keyEndTime, keyEndTime - keyStartTime);
        private ShaderProperty<Vector4> KeyTime;
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
            KeyTime = new ShaderProperty<Vector4>(nameof(KeyTime));
            DisplacementAxes = new ShaderProperty<Vector4[]>(nameof(DisplacementAxes));
            Thickness = new ShaderProperty<float>(nameof(Thickness));
            WaveOriginToWorldMatrix = new ShaderProperty<Matrix4x4>(nameof(WaveOriginToWorldMatrix));
            WorldToWaveOriginMatrix = new ShaderProperty<Matrix4x4>(nameof(WorldToWaveOriginMatrix));

            //states = GetComponentsInChildren<WaveState>().ToList().Select(behaviour => behaviour.State).ToList();
            Assert.AreNotEqual(states.Count, 0);
            Assert.AreNotEqual(states.Count, 1);
            numberOfWaves = states.Count;
            states.ForEach(state => Assert.AreEqual(states.Count, numberOfWaves));
            
            SetDynamicProperties(states[currentIndex], states[currentIndex + 1]);
        }

        private bool DurationMet() => Time.timeSinceLevelLoad - lastSetTime >= states[currentIndex].Duration;
        private bool OnLastIndex() => currentIndex == states.Count - 1;

        private void Update()
        {
            KeyFrame current = states[currentIndex];
            
            if (!OnLastIndex() && DurationMet())
            {
                current = states[++currentIndex];
                SetDynamicProperties(current, OnLastIndex() ? current : states[currentIndex + 1]);
            }

            Bounds bounds = new Bounds(transform.position, Vector3.one * 50f);
            int numberOfVertices = 24 * (int)(current.Time / (1f / current.SampleRate));
            Graphics.DrawProcedural(material, bounds, MeshTopology.Triangles, numberOfVertices, 0, null, propertyBlock, ShadowCastingMode.TwoSided);
        }

        private void SetDynamicProperties(KeyFrame initial, KeyFrame target)
        {
            lastSetTime = Time.timeSinceLevelLoad;
            Vector4[] waveData = new Vector4[numberOfWaves * 2];
            Vector4[] displacementAxes = new Vector4[numberOfWaves * 2];
            
            for (var waveIndex = 0; waveIndex < numberOfWaves; waveIndex++)
            {
                int dataIndex = 2 * waveIndex;
                Wave start = initial.Waves[waveIndex];
                Wave end = target.Waves[waveIndex];

                waveData[dataIndex] = new Vector4(start.Frequency, start.Amplitude, start.PhaseOffset, (float)start.WaveType);
                displacementAxes[dataIndex] = new float4(initial.Waves[waveIndex].DisplacementAxis, 0f);
                waveData[dataIndex + 1] = new Vector4(end.Frequency, end.Amplitude, end.PhaseOffset, (float)end.WaveType);
                displacementAxes[dataIndex + 1] = new float4(initial.Waves[waveIndex].DisplacementAxis, 0f);
            }
            
            propertyBlock.SetProperty(KeyTime, new float4(Time.timeSinceLevelLoad,
                                                          Time.timeSinceLevelLoad + initial.Duration,
                                                          initial.Duration,
                                                          0f));
            
            propertyBlock.SetProperty(SampleRate, initial.SampleRate);
            propertyBlock.SetProperty(WaveCount, numberOfWaves);
            propertyBlock.SetProperty(WaveData, waveData);
            propertyBlock.SetProperty(DisplacementAxes, displacementAxes);
            propertyBlock.SetProperty(Thickness, initial.Thickness);
            propertyBlock.SetProperty(PropogationScaleProperty, 1.0f);
            propertyBlock.SetProperty(DisplacementScaleProperty, 1.0f);
            propertyBlock.SetProperty(WaveOriginToWorldMatrix, transform.localToWorldMatrix);
            propertyBlock.SetProperty(WorldToWaveOriginMatrix, transform.worldToLocalMatrix);
        }
    }
}