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
        Tags
        {
            "RenderType"="Opaque"
        }
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

        // Animatable Sample Rate
        float SampleRateAnimation;
        float SampleRateFrom;
        float SampleRateTo;

        // Animatable Thickness
        float ThicknessAnimation;
        float ThicknessFrom;
        float ThicknessTo;

        // Animatable Signal Length
        float SignalLengthAnimation;
        float SignalLengthFrom;
        float SignalLengthTo;

        // Animatable Projection
        float ProjectionAnimation;
        float ProjectionFrom;
        float ProjectionTo;
        
        float StartTime;
        float EndTime;

        int WaveCount;
        float4x4 WaveTransitionData[NumberOfSupportedWaves];

        float WaveAxesData[NumberOfSupportedWaves * 3 * 2];

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
            const float timeDelta = EndTime - StartTime;
            const float lerpTime = smoothstep(0, 1, (_Time.y - StartTime) / timeDelta);

            const float time = GetTimeForVertexIndex(appdata.vid, SampleRateFrom);
            const float timeResolution = GetTimeResolution(SampleRateFrom);
            const float3 forward = mul(WaveOriginToWorldMatrix, float3(0, 0, 1));

            float3 samplePosition, nextSamplePosition;
            float3 sampleTangent, nextSampleTangent;
            for (int index = 0; index < WaveCount * 2; index += 2)
            {
                const float4x4 data = WaveTransitionData[index];
                
                const float3 startFreqAmpPhase = data[0].xyz;
                const float3 endFreqAmpPhase = data[2].xyz;
                const float3 currentFreqAmpPhase = lerp(startFreqAmpPhase, endFreqAmpPhase, lerpTime);
                const float animation = data[0].w;

                const float4 startWaveTypeRatio = data[1];
                const float4 endWaveTypeRatio = data[3];
                const float4 currentWaveTypeRatio = lerp(startWaveTypeRatio, endWaveTypeRatio, lerpTime);

                const int axisIndex = index * 3 * 2;
                const float3 startAxis = float3(WaveAxesData[axisIndex + 0], WaveAxesData[axisIndex+1], WaveAxesData[axisIndex+2]);
                const float3 endAxis = float3(WaveAxesData[axisIndex+3], WaveAxesData[axisIndex+4], WaveAxesData[axisIndex+5]);
                
                const float frequency = currentFreqAmpPhase.x;
                const float amplitude = currentFreqAmpPhase.y;
                const float phase = currentFreqAmpPhase.z;
                const float3 displacementAxis = lerp(startAxis, endAxis,lerpTime);
                const Wave wave = ConstructWave(frequency, amplitude, phase, currentWaveTypeRatio);
                AppendPositionAndTangent(time, timeResolution, wave, forward, displacementAxis, samplePosition,
                                         nextSamplePosition, sampleTangent, nextSampleTangent);
            }

            samplePosition = samplePosition - forward * (WaveCount - 1);
            nextSamplePosition = nextSamplePosition - forward * (WaveCount - 1);

            sampleTangent = normalize(sampleTangent);
            nextSampleTangent = normalize(nextSampleTangent);

            float3 normal;
            const float3 localPosition = GetVertexPosition(appdata.vid, samplePosition, nextSamplePosition,
                                                           sampleTangent, nextSampleTangent, ThicknessFrom, normal);

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