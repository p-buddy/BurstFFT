Shader "JamUp/ProdeduralWave"
{
  //show values to edit in inspector
  Properties
  {
    _Color ("Color", Color) = (0, 0, 0, 1)
  }

  SubShader
  {
      //the material is completely non-transparent and is rendered at the same time as the other opaque geometry
      Tags{ "RenderType"="Opaque" }
      Cull Off
    
      CGPROGRAM
      //include useful shader functions
      #include "UnityCG.cginc"
      #include "WaveFunctions.cginc"

      //define vertex and fragment shader functions
      #pragma surface surf Standard vertex:vert addshadow
      #pragma target 3.5

      const static int NumberOfSupportedWaves = 10;

      //tint of the texture
      
      
      /* BEGIN SHADER SETTINGS */
      half4 _Color;
      half Smoothness;
      half Metallic;
      half3 Emission;
      
      float4x4 WaveOriginToWorldMatrix;
      float4x4 WorldToWaveOriginMatrix;
      
      int SampleRate;
      int WaveCount;
      float Thickness;

      float Frequencies[NumberOfSupportedWaves];
      float Amplitudes[NumberOfSupportedWaves];
      float Phases[NumberOfSupportedWaves];
      int WaveTypes[NumberOfSupportedWaves];

      float4 DisplacementAxes[NumberOfSupportedWaves];
      float PropagationScale;
      float DisplacementScale;
      /* END SHADER SETTINGS */

      struct VertData
      {
          float4 vertex : POSITION;
          float3 normal : NORMAL;
          float4 tangent : TANGENT;
          float4 color : COLOR;
          float4 texcoord1 : TEXCOORD1;
          float4 texcoord2 : TEXCOORD2;
          uint vid : SV_VertexID;
      };

      struct Input
      {
          float vface : VFACE;
          float4 color : COLOR;
      };

      //the vertex shader function
      void vert(inout VertData appdata)
      {
        const float time = GetTimeForVertexIndex(appdata.vid, SampleRate);
        const float timeResolution = GetTimeResolution(SampleRate);
        const float3 forward = mul(WaveOriginToWorldMatrix, float3(0, 0, 1));

        float3 samplePosition, nextSamplePosition;
        float3 sampleTangent, nextSampleTangent;
        for (int index = 0; index < WaveCount; index++)
        {
            const float3 displacementAxis = DisplacementAxes[index].xyz;
            const Wave wave = ConstructWave(Frequencies[index], Amplitudes[index], Phases[index], WaveTypes[index]);
            AppendPositionAndTangent(time, timeResolution, wave, forward, displacementAxis, samplePosition, nextSamplePosition, sampleTangent, nextSampleTangent);
        }
        
        samplePosition = samplePosition - forward * (WaveCount - 1);
        nextSamplePosition = nextSamplePosition - forward * (WaveCount - 1);
        
        sampleTangent = normalize(sampleTangent);
        nextSampleTangent = normalize(nextSampleTangent);

        float3 normal;
        const float3 localPosition = GetVertexPosition(appdata.vid, samplePosition, nextSamplePosition, sampleTangent, nextSampleTangent, Thickness, normal);
        
        appdata.vertex.xyz = localPosition;
        appdata.normal = normal;
        appdata.tangent.xyz = sampleTangent;
        appdata.color = float4(nextSampleTangent, 1);
          
        // Transform modification
        unity_ObjectToWorld = WaveOriginToWorldMatrix;
        unity_WorldToObject = WorldToWaveOriginMatrix;
      }

      void surf(Input IN, inout SurfaceOutputStandard o)
      {
            o.Albedo = IN.color.rgb;
            o.Metallic = Metallic;
            o.Smoothness = Smoothness;
            o.Emission = Emission * IN.color.rgb;
      }
      ENDCG
    
  }
  Fallback "VertexLit"
}