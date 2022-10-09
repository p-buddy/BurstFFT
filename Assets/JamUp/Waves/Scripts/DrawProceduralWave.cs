using System;
using System.Linq;
using JamUp.UnityUtility;
using JamUp.Waves.Scripts.API;
using JamUp.Waves.Scripts;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace JamUp.Waves.Scripts
{
    public class DrawProceduralWave : MonoBehaviour
    {
        #if false
        [TextArea(50, int.MaxValue)]
        public string TestField;
        private Camera camera;
        private CameraSettings settings;

        private KeyFrame[] states;
        private int currentIndex;
        private int numberOfWaves;
        private float lastSetTime;

        [SerializeField]
        private Material material;
        
        MaterialPropertyBlock propertyBlock;

        private ShaderProperty<int> WaveCount;
        private ShaderProperty<int> SampleRate;
        private ShaderProperty<Vector4[]> WaveData;
        private ShaderProperty<Vector4> KeyTime;
        private ShaderProperty<float> PropogationScaleProperty;
        private ShaderProperty<float> DisplacementScaleProperty;
        private ShaderProperty<Matrix4x4> WaveOriginToWorldMatrix;
        private ShaderProperty<Matrix4x4>  WorldToWaveOriginMatrix;
        private ShaderProperty<Vector4[]> DisplacementAxes;
        private ShaderProperty<float> Thickness;

        private void Start()
        {
            camera = UnityEngine.Camera.main;
            settings = CameraSettings.Default;
            
            propertyBlock = new MaterialPropertyBlock();
            WaveCount = new ShaderProperty<int>(nameof(WaveCount));
            SampleRate = new ShaderProperty<int>(nameof(SampleRate));
            WaveData = new ShaderProperty<Vector4[]>(nameof(WaveData));
            KeyTime = new ShaderProperty<Vector4>(nameof(KeyTime));
            DisplacementAxes = new ShaderProperty<Vector4[]>(nameof(DisplacementAxes));
            Thickness = new ShaderProperty<float>(nameof(Thickness));
            WaveOriginToWorldMatrix = new ShaderProperty<Matrix4x4>(nameof(WaveOriginToWorldMatrix));
            WorldToWaveOriginMatrix = new ShaderProperty<Matrix4x4>(nameof(WorldToWaveOriginMatrix));
            
            states = BareBonesAPI.GetTestFrames(TestField);
            Assert.AreNotEqual(states.Length, 0);
            numberOfWaves = states[0].Waves.Length;
            states.ToList().ForEach(state => Assert.AreEqual(state.Waves.Length, numberOfWaves));

            KeyFrame nextState = currentIndex + 1 >= states.Length ? states[currentIndex] : states[currentIndex + 1];
            SetDynamicProperties(states[currentIndex], nextState);
        }

        private void OnDestroy()
        {
            CameraHelper.Dispose();
        }

        private bool DurationMet() => Time.timeSinceLevelLoad - lastSetTime >= states[currentIndex].Duration;
        private bool OnLastIndex() => currentIndex == states.Length - 1;

        private void Update()
        {
            bool isLast = OnLastIndex();
            KeyFrame current = states[currentIndex];
            KeyFrame next = isLast ? current : states[currentIndex + 1];
            
            if (!OnLastIndex() && DurationMet())
            {
                current = states[++currentIndex];
                next = OnLastIndex() ? current : states[currentIndex + 1];
                SetDynamicProperties(current, next);
            }

            Bounds bounds = new Bounds(transform.position, Vector3.one * 50f);
            
            float lerpTime = (Time.timeSinceLevelLoad - lastSetTime) / states[currentIndex].Duration;
            int sampleRate = current.SampleRate + (int)((next.SampleRate - current.SampleRate) * lerpTime);
            float time = current.SignalLength + (next.SignalLength - current.SignalLength) * lerpTime;
            int numberOfVertices = 24 * (int)(time / (1f / sampleRate));
            CameraHelper.LerpProjection(camera, current.ProjectionType, next.ProjectionType, lerpTime, in settings);
            Graphics.DrawProcedural(material, bounds, MeshTopology.Triangles, numberOfVertices, 0, null, propertyBlock, ShadowCastingMode.TwoSided);
        }
        
        // 
        
        // Entity:
        // CurrentTransition
        // - WaveStart
        // - WaveEnd
        // Transition Index
        // - Keyframes
        
        
        
        // Every Entity Has a Buffer Of Keyframes
        // Every Keyframe buffer references an entity that's a buffer of waves
        // Or should there be a transition component? And there's a buffer of transitions?

        private void SetDynamicProperties(KeyFrame initial, KeyFrame target)
        {
            lastSetTime = Time.timeSinceLevelLoad;
            Vector4[] waveData = new Vector4[numberOfWaves * 2];
            Vector4[] displacementAxes = new Vector4[numberOfWaves * 2];
            
            for (var waveIndex = 0; waveIndex < numberOfWaves; waveIndex++)
            {
                int dataIndex = 2 * waveIndex;
                Wave start = (WaveState)initial.Waves[waveIndex];
                Wave end = (WaveState)target.Waves[waveIndex];

                waveData[dataIndex] = new Vector4(start.Frequency, start.Amplitude, start.PhaseOffset, (float)start.WaveType);
                displacementAxes[dataIndex] = new float4(initial.Waves[waveIndex].Value.DisplacementAxis, 0f);
                waveData[dataIndex + 1] = new Vector4(end.Frequency, end.Amplitude, end.PhaseOffset, (float)end.WaveType);
                displacementAxes[dataIndex + 1] = new float4(target.Waves[waveIndex].Value.DisplacementAxis, 0f);
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
        #endif
    }
}