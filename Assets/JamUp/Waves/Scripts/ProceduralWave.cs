using System.Numerics;
using JamUp.UnityUtility;

namespace JamUp.Waves.Scripts
{
    public readonly struct ProceduralWave
    {
        private readonly ShaderProperty<int> waveCount;
        private readonly ShaderProperty<int> sampleRate;
        private readonly ShaderProperty<Vector4[]> waveData;
        private readonly ShaderProperty<Vector4> keyTime;
        private readonly ShaderProperty<float> propagationScaleProperty;
        private readonly ShaderProperty<float> displacementScaleProperty;
        private readonly ShaderProperty<Matrix4x4> waveOriginToWorldMatrix;
        private readonly ShaderProperty<Matrix4x4>  worldToWaveOriginMatrix;
        private readonly ShaderProperty<Vector4[]> displacementAxes;
        private readonly ShaderProperty<float> thickness;
    }
}