using System;
using JamUp.UnityUtility;
using JamUp.Waves;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

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
        private ShaderProperty WorldToWaveOriginMatrixProperty;
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
            WorldToWaveOriginMatrixProperty = new ShaderProperty(nameof(WorldToWaveOriginMatrixProperty), "Property");
        }

        private void Update()
        {
            if (state.serializableWaves.Length == 0)
            {
                return;
            }
            SetDynamicProperties();
            Bounds bounds = new Bounds(transform.position, Vector3.one * 50f);
            int numberOfVertices = 24 * (int)(state.time / (1f / state.sampleRate));
            Graphics.DrawProcedural(material, bounds, MeshTopology.Triangles, numberOfVertices, 0, null, propertyBlock, ShadowCastingMode.TwoSided);
            propertyBlock.Clear();
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
                displacementAxes[index] = new float4(state.serializableWaves[index].DisplacementAxis, 0f);
            }

            propertyBlock.SetInt(SampleRateProperty.ID, state.sampleRate);
            propertyBlock.SetInt(WaveCountProperty.ID, waveCount);
            propertyBlock.SetFloatArray(FrequenciesProperty.ID, frequencies);
            propertyBlock.SetFloatArray(AmplitudesProperty.ID, amplitudes);
            propertyBlock.SetFloatArray(PhasesProperty.ID, phases);
            propertyBlock.SetFloatArray(WaveTypesProperty.ID, waveTypes);
            propertyBlock.SetVectorArray(DisplacementAxesProperty.ID, displacementAxes);
            propertyBlock.SetFloat(ThicknessProperty.ID, state.thickness);
            propertyBlock.SetFloat(PropogationScaleProperty.ID, 1.0f);
            propertyBlock.SetFloat(DisplacementScaleProperty.ID, 1.0f);
            propertyBlock.SetMatrix(WaveOriginToWorldMatrixProperty.ID, transform.localToWorldMatrix);
            propertyBlock.SetMatrix(WorldToWaveOriginMatrixProperty.ID, transform.worldToLocalMatrix);
        }
    }
}