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
      float elapsedTime;
      
      float4x4 WaveOriginToWorldMatrix;
      float4x4 WorldToWaveOriginMatrix;

      // (SampleRate, Thickness, Smoothness)
      float4 GlobalData[2];
      int SampleRate;
      float Thickness;

      int WaveCount;


      // (keyStartTime, keyEndTime, keyEndTime - keyStartTime);
      float4 KeyTime;
      float4 WaveData[NumberOfSupportedWaves * 2];
      float4 DisplacementAxes[NumberOfSupportedWaves * 2];
      
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

        const float initialTime = KeyTime.x, timeDelta = KeyTime.z;
        const float lerpTime = smoothstep(0, 1, (_Time.y - initialTime) / timeDelta);
          
        float3 samplePosition, nextSamplePosition;
        float3 sampleTangent, nextSampleTangent;
        for (int index = 0; index < WaveCount * 2; index += 2)
        {
            const float4 initial = WaveData[index];
            const float4 target = WaveData[index + 1];
            const float4 current = lerp(initial, target, lerpTime);
            const float frequency = current.x, amplitude = current.y, phase = current.z;
            const int type = initial.w;
            const float3 displacementAxis = DisplacementAxes[index].xyz;
            const Wave wave = ConstructWave(frequency, amplitude, phase, type);
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
        elapsedTime += unity_DeltaTime.x;  
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