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

      //tint of the texture
      half4 _Color;
      half _Smoothness;
      half _Metallic;
      half3 _Emission;

      const static int NumberOfSupportedWaves = 10;
      float4x4 WaveOriginToWorldMatrix;
      float4x4 WorldToWaveOriginMatrix;
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

        float3 position, nextPosition;
        float3 tangent, nextTangent;
        for (int index = 0; index < WaveCount; index++)
        {
            const float3 displacementAxis = DisplacementAxes[index].xyz;
            const Wave wave = ConstructWave(Frequencies[index], Amplitudes[index], Phases[index], WaveTypes[index]);
            AppendPositionAndTangent(time, timeResolution, wave, forward, displacementAxis, position, nextPosition, tangent, nextTangent);
        }
        
        position = position - forward * (WaveCount - 1);
        nextPosition = nextPosition - forward * (WaveCount - 1);
        //position = mul(WaveOriginToWorldMatrix, float4(position, 1));
        //nextPosition = mul(WaveOriginToWorldMatrix, float4(nextPosition, 1));
        
        tangent = normalize(tangent);
        nextTangent = normalize(nextTangent);
        //tangent = mul(WaveOriginToWorldMatrix, tangent);
        //nextTangent = mul(WaveOriginToWorldMatrix, nextTangent);
        
        const float3 localPosition = GetVertexPosition(appdata.vid, position, nextPosition, tangent, nextTangent, Thickness);
        appdata.vertex.xyz = localPosition;
        appdata.normal = float3(normalize(localPosition - position));
        appdata.tangent.xyz = tangent;
        appdata.color = _Color;
          
        // Transform modification
        unity_ObjectToWorld = WaveOriginToWorldMatrix;
        unity_WorldToObject = WorldToWaveOriginMatrix;
      }

      void surf(Input IN, inout SurfaceOutputStandard o)
      {
            o.Albedo = _Color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Normal = float3(0, 0, IN.vface < 0 ? -1 : 1); // back face support
            o.Emission = _Emission * IN.color.rgb;
      }


      ENDCG
    
  }
  Fallback "VertexLit"
}