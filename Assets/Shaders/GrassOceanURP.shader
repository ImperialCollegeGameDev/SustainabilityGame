Shader "URP/Environment/GrassOcean"
{
    Properties
    {
        _LowColor("Low Color", Color) = (0.25, 0.7, 0.25, 1)
        _HighColor("High Color", Color) = (0.6, 0.95, 0.45, 1)
        _SlopeColor("Slope Color", Color) = (0.6, 0.65, 0.4, 1)

        _HeightMin("Height Min", Float) = 0.0
        _HeightMax("Height Max", Float) = 20.0

        _StripeScale("Stripe Scale", Float) = 0.35
        _WaveScale("Wave Scale", Float) = 0.08
        _WaveSpeed("Wave Speed", Float) = 0.7
        _WaveStrength("Wave Strength", Float) = 0.6

        _WindDirection("Wind Direction XZ", Vector) = (1, 0, 0.35, 0)

        _NoiseScale("Noise Scale", Float) = 0.18
        _NoiseStrength("Noise Strength", Range(0,1)) = 0.18

        _Smoothness("Smoothness", Range(0,1)) = 0.1
        _Metallic("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _LowColor;
                float4 _HighColor;
                float4 _SlopeColor;
                float  _HeightMin;
                float  _HeightMax;
                float  _StripeScale;
                float  _WaveScale;
                float  _WaveSpeed;
                float  _WaveStrength;
                float4 _WindDirection;
                float  _NoiseScale;
                float  _NoiseStrength;
                float  _Smoothness;
                float  _Metallic;
            CBUFFER_END

<<<<<<< Updated upstream
            float hash31(float3 p)
            {
                p = frac(p * 0.3183099 + float3(0.1, 0.2, 0.3));
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = NormalizeNormalPerVertex(normInputs.normalWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 wsPos = IN.positionWS;
                float3 n = normalize(IN.normalWS);

                float h01 = saturate((wsPos.y - _HeightMin) / max(0.0001, (_HeightMax - _HeightMin)));
                float slope = saturate(dot(n, float3(0,1,0)));

                float3 baseH = lerp(_LowColor.rgb, _HighColor.rgb, h01);
                float3 baseColor = lerp(_SlopeColor.rgb, baseH, slope);

                float2 dirXZ = normalize(_WindDirection.xz);
                if (all(dirXZ == 0))
                    dirXZ = float2(1, 0);

                float wavePhase = dot(wsPos.xz, dirXZ * _WaveScale) + _Time.y * _WaveSpeed;
                float wave = sin(wavePhase);
                float offset = wave * _WaveStrength;

                float2 stripePos = wsPos.xz + dirXZ * offset;
                float stripeCoord = dot(stripePos, dirXZ * _StripeScale);
                float stripe = sin(stripeCoord);
                float stripeMask = 0.5 + 0.5 * stripe;

                float3 noisePos = wsPos * _NoiseScale;
                float nVal = hash31(floor(noisePos));
                nVal = nVal * 2.0 - 1.0;

                float brightness = lerp(0.9, 1.2, stripeMask);
                brightness += nVal * _NoiseStrength;

                float flatness = saturate(pow(slope, 3.0));
                float3 modulated = baseColor * brightness;
                float3 colorGrass = lerp(baseColor, modulated, flatness);

                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);

                float3 bendDir = float3(dirXZ.x, 0, dirXZ.y);
                float3 bendN = normalize(n + bendDir * offset * 0.2);

                float NdotL = saturate(dot(bendN, -L));
                float3 diffuse = colorGrass * (NdotL * mainLight.color.rgb);
                float3 ambient = colorGrass * 0.4;

                float3 finalCol = diffuse + ambient;
                return half4(finalCol, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

Shader "URP/Environment/GrassOcean"
{
    Properties
    {
        _LowColor("Low Color", Color) = (0.25, 0.7, 0.25, 1)
        _HighColor("High Color", Color) = (0.6, 0.95, 0.45, 1)
        _SlopeColor("Slope Color", Color) = (0.6, 0.65, 0.4, 1)

        _HeightMin("Height Min", Float) = 0.0
        _HeightMax("Height Max", Float) = 20.0

        _StripeScale("Stripe Scale", Float) = 0.35
        _WaveScale("Wave Scale", Float) = 0.08
        _WaveSpeed("Wave Speed", Float) = 0.7
        _WaveStrength("Wave Strength", Float) = 0.6

        _WindDirection("Wind Direction XZ", Vector) = (1, 0, 0.35, 0)

        _NoiseScale("Noise Scale", Float) = 0.18
        _NoiseStrength("Noise Strength", Range(0,1)) = 0.18

        _Smoothness("Smoothness", Range(0,1)) = 0.1
        _Metallic("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _LowColor;
                float4 _HighColor;
                float4 _SlopeColor;
                float  _HeightMin;
                float  _HeightMax;
                float  _StripeScale;
                float  _WaveScale;
                float  _WaveSpeed;
                float  _WaveStrength;
                float4 _WindDirection;
                float  _NoiseScale;
                float  _NoiseStrength;
                float  _Smoothness;
                float  _Metallic;
            CBUFFER_END

=======
>>>>>>> Stashed changes
            // simple hash noise
            float hash31(float3 p)
            {
                p = frac(p * 0.3183099 + float3(0.1, 0.2, 0.3));
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = NormalizeNormalPerVertex(normInputs.normalWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 wsPos = IN.positionWS;
                float3 n = normalize(IN.normalWS);

                // height-based color
                float h01 = saturate((wsPos.y - _HeightMin) / max(0.0001, (_HeightMax - _HeightMin)));

                // slope factor: 1 = flat, 0 = vertical
                float slope = saturate(dot(n, float3(0,1,0)));

                float3 baseH = lerp(_LowColor.rgb, _HighColor.rgb, h01);
                float3 baseColor = lerp(_SlopeColor.rgb, baseH, slope);

                // wind direction
                float2 dirXZ = normalize(_WindDirection.xz);
                if (all(dirXZ == 0))
                    dirXZ = float2(1, 0);

                // wave offset along wind direction (ocean-like stripes)
                float wavePhase = dot(wsPos.xz, dirXZ * _WaveScale) + _Time.y * _WaveSpeed;
                float wave = sin(wavePhase);
                float offset = wave * _WaveStrength;

                // stripe coordinate along wind dir, shifted by wave
                float2 stripePos = wsPos.xz + dirXZ * offset;
                float stripeCoord = dot(stripePos, dirXZ * _StripeScale);
                float stripe = sin(stripeCoord);
                float stripeMask = 0.5 + 0.5 * stripe;

                // noise to break uniformity
                float3 noisePos = wsPos * _NoiseScale;
                float nVal = hash31(floor(noisePos));
                nVal = nVal * 2.0 - 1.0;

                float brightness = lerp(0.9, 1.2, stripeMask);
                brightness += nVal * _NoiseStrength;

                // only keep strong modulation on relatively flat ground
                float flatness = saturate(pow(slope, 3.0)); // flat => 1, steep => 0
                float3 modulated = baseColor * brightness;
                float3 colorGrass = lerp(baseColor, modulated, flatness);

                // simple lighting
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);

                float3 bendDir = float3(dirXZ.x, 0, dirXZ.y);
                float3 bendN = normalize(n + bendDir * offset * 0.2);

                float NdotL = saturate(dot(bendN, -L));
                float3 diffuse = colorGrass * (NdotL * mainLight.color.rgb);
                float3 ambient = colorGrass * 0.4;

                float3 finalCol = diffuse + ambient;
                return half4(finalCol, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

