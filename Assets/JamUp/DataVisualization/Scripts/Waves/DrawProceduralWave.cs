using System;
using JamUp.UnityUtility;
using JamUp.Waves;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.DataVisualization.Waves
{
    public class DrawProceduralWave : MonoBehaviour
    {
        [SerializeField] 
        private WaveDrawingState state;

        [SerializeField]
        private Material material;
        
        MaterialPropertyBlock propertyBlock;

        private ShaderProperty WaveCountProperty;
        private ShaderProperty SampleRateProperty;
        private ShaderProperty FrequenciesProperty;
        private ShaderProperty AmplitudesProperty;
        private ShaderProperty PhasesProperty;
        private ShaderProperty WaveTypesProperty;
        private ShaderProperty PropogationScaleProperty;
        private ShaderProperty DisplacementScaleProperty;
        private ShaderProperty WaveOriginToWorldMatrixProperty;
        private ShaderProperty DisplacementAxesProperty;
        private ShaderProperty ThicknessProperty;

        private void Start()
        {
            propertyBlock = new MaterialPropertyBlock();
            WaveCountProperty = new ShaderProperty(nameof(WaveCountProperty), "Property");
            SampleRateProperty = new ShaderProperty(nameof(SampleRateProperty), "Property");
            FrequenciesProperty = new ShaderProperty(nameof(FrequenciesProperty), "Property");
            AmplitudesProperty = new ShaderProperty(nameof(AmplitudesProperty), "Property");
            PhasesProperty = new ShaderProperty(nameof(PhasesProperty), "Property");
            WaveTypesProperty = new ShaderProperty(nameof(WaveTypesProperty), "Property");
            DisplacementAxesProperty = new ShaderProperty(nameof(DisplacementAxesProperty), "Property");
            ThicknessProperty = new ShaderProperty(nameof(ThicknessProperty), "Property");
            WaveOriginToWorldMatrixProperty = new ShaderProperty(nameof(WaveOriginToWorldMatrixProperty), "Property");
        }

        private void Update()
        {
            if (state.serializableWaves.Length == 0)
            {
                return;
            }
            SetDynamicProperties();
            Bounds bounds = new Bounds(transform.position, Vector3.one * 50f);
            int sampleRate = 100;
            int numberOfVertices = 24 * (sampleRate - 1);
            Graphics.DrawProcedural(material, bounds, MeshTopology.Triangles, numberOfVertices, 0, Camera.main, propertyBlock);
        }

        private void LateUpdate()
        {
            propertyBlock.Clear();
        }
        
        private void SetConstantProperties()
        {
        }

        private void SetDynamicProperties()
        {
            int waveCount = state.serializableWaves.Length;
            float[] frequencies = new float[waveCount];
            float[] amplitudes = new float[waveCount];
            float[] phases = new float[waveCount];
            float[] waveTypes = new float[waveCount];
            Vector4[] displacementAxes = new Vector4[waveCount];
            for (var index = 0; index < state.serializableWaves.Length; index++)
            {
                Wave wave = state.serializableWaves[index].Wave;
                frequencies[index] = wave.Frequency;
                amplitudes[index] = wave.Amplitude;
                phases[index] = wave.PhaseOffset;
                waveTypes[index] = (float)wave.WaveType;
                displacementAxes[index] = math.up().xyzx;
            }

            propertyBlock.SetInt(SampleRateProperty.ID, 100);
            propertyBlock.SetInt(WaveCountProperty.ID, waveCount);
            propertyBlock.SetFloatArray(FrequenciesProperty.ID, frequencies);
            propertyBlock.SetFloatArray(AmplitudesProperty.ID, amplitudes);
            propertyBlock.SetFloatArray(PhasesProperty.ID, phases);
            propertyBlock.SetFloatArray(WaveTypesProperty.ID, waveTypes);
            propertyBlock.SetVectorArray(DisplacementAxesProperty.ID, displacementAxes);
            propertyBlock.SetFloat(ThicknessProperty.ID, 1.0f);
            propertyBlock.SetFloat(PropogationScaleProperty.ID, 1.0f);
            propertyBlock.SetFloat(DisplacementScaleProperty.ID, 1.0f);
            propertyBlock.SetMatrix(WaveOriginToWorldMatrixProperty.ID, transform.localToWorldMatrix);
        }
    }
}