Shader "JamUp/ProdeduralWave"
{
  //show values to edit in inspector
  Properties
  {
    _Color ("Color", Color) = (0, 0, 0, 1)
  }

  SubShader{
    //the material is completely non-transparent and is rendered at the same time as the other opaque geometry
    Tags{ "RenderType"="Opaque" "Queue"="Geometry" }

    Pass
    {
      CGPROGRAM

      /* BEGIN DECLARATION OF DEBUG BUFFERS */
      RWStructuredBuffer<float> DEBUG_DIRTY_FLAG_BUFFER;
      RWStructuredBuffer<float3> DEBUG_VERTS_BUFFER;
      /* END DECLARATION OF DEBUG BUFFERS */

      //include useful shader functions
      #include "UnityCG.cginc"
      #include "WaveFunctions.cginc"

      //define vertex and fragment shader functions
      #pragma vertex vert
      #pragma fragment frag
      #pragma target 3.5

      //tint of the texture
      fixed4 _Color;

      const static int NumberOfSupportedWaves = 10;
      float4x4 WaveOriginToWorldMatrix;
      int SampleRate;
      int WaveCount;
      float Thickness;

      // Wave Settings
      float Frequencies[NumberOfSupportedWaves];
      float Amplitudes[NumberOfSupportedWaves];
      float Phases[NumberOfSupportedWaves];
      int WaveTypes[NumberOfSupportedWaves];

      float4 DisplacementAxes[NumberOfSupportedWaves];
      
      float PropagationScale;
      float DisplacementScale;

      //the vertex shader function
      float4 vert(uint vertex_id: SV_VertexID) : SV_POSITION
      {
        const float time = GetTimeForVertexIndex(vertex_id, SampleRate);
        const float timeResolution = GetTimeResolution(SampleRate);
        const float3 forward = mul(WaveOriginToWorldMatrix, float3(0, 0, 1));

        float3 position, nextPosition;
        float3 tangent, nextTangent;
        for (int index = 0; index < NumberOfSupportedWaves; index++)
        {
          const float3 displacementAxis = DisplacementAxes[index].xyz;
          const Wave wave = NewWave(Frequencies[index], Amplitudes[index], Phases[index], WaveTypes[index]);
          
          position += GetValueAtTime(wave, time, displacementAxis);
          nextPosition += GetValueAtTime(wave, time + timeResolution, displacementAxis);
          
          tangent += GetTangent(wave, time, timeResolution, displacementAxis, forward);
          nextTangent += GetTangent(wave, time + timeResolution, timeResolution, displacementAxis, forward);
        }
        tangent = normalize(tangent);
        nextTangent = normalize(nextTangent);
        position = mul(WaveOriginToWorldMatrix, position);
        nextPosition = mul(WaveOriginToWorldMatrix, nextPosition);
        
        const float3 worldPosition = GetVertexPosition(vertex_id, position, nextPosition, tangent, nextTangent, Thickness);
        DEBUG_VERTS_BUFFER[vertex_id] = worldPosition;
        DEBUG_DIRTY_FLAG_BUFFER[0] = 1;
        return mul(UNITY_MATRIX_VP, worldPosition);
      }

      //the fragment shader function
      fixed4 frag() : SV_TARGET
      {
        //return the final color to be drawn on screen
        return _Color;
      }

      ENDCG
    }
  }
  Fallback "VertexLit"
}