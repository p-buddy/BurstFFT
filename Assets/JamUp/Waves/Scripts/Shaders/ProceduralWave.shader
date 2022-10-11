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
        int SampleRateAnimation;
        int SampleRateFrom;
        int SampleRateTo;

        // Animatable Thickness
        float ThicknessAnimation;
        float ThicknessFrom;
        float ThicknessTo;

        // Animatable Signal Length
        int SignalLengthAnimation;
        int SignalLengthFrom;
        int SignalLengthTo;

        // Animatable Projection
        float ProjectionAnimation;
        float ProjectionFrom;
        float ProjectionTo;
        
        float StartTime;
        float EndTime;

        int WaveCount;
        float4x4 WaveTransitionData[NumberOfSupportedWaves];

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
                const float4 startWave = data[0];
                const float3 startAxis = data[1].xyz;
                const float animation = data[1].w;
                const float4 endWave = data[2];
                const float3 endAxis = data[3];
                
                const float4 current = lerp(startWave, endWave, lerpTime);
                const float frequency = current.x, amplitude = current.y, phase = current.z;
                const float3 displacementAxis = lerp(startAxis, endAxis,lerpTime);
                const Wave wave = ConstructWave(frequency, amplitude, phase, startWave.w, endWave.w, lerpTime);
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