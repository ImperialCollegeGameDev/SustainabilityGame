Shader "URP/Environment/GrassCards"
{
    Properties
    {
        _MainTex("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        _Color("Tint Color", Color) = (0.45, 0.95, 0.5, 1)
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.3

        _WindDirection("Wind Direction XZ", Vector) = (1, 0, 0.3, 0)
        _WindStrength("Wind Strength", Float) = 0.18
        _WindSpeed("Wind Speed", Float) = 1.0
        _WindScale("Wind Scale", Float) = 0.5
        _BendFactor("Bend Factor (height)", Float) = 1.2
        _Turbulence("Turbulence", Float) = 0.3

        _Smoothness("Smoothness", Range(0,1)) = 0.15
        _Metallic("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
        }

        LOD 250
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

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
                float2 uv         : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _Cutoff;
                float4 _WindDirection;
                float  _WindStrength;
                float  _WindSpeed;
                float  _WindScale;
                float  _BendFactor;
                float  _Turbulence;
                float  _Smoothness;
                float  _Metallic;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // simple 3D hash noise
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

                float3 worldPos = posInputs.positionWS;

                // wind direction XZ
                float2 dirXZ = normalize(_WindDirection.xz);
                if (all(dirXZ == 0))
                {
                    dirXZ = float2(1, 0);
                }

                // height-based bending: bottom几乎不动，顶部弯曲最多
                float height01 = saturate(IN.positionOS.y * _BendFactor);

                // base wind wave
                float wave = dot(worldPos.xz, dirXZ * _WindScale) + _Time.y * _WindSpeed;
                float sway = sin(wave) * _WindStrength * height01;

                // turbulence: 给不同位置一点相位差
                float turb = (hash31(worldPos * 0.5) - 0.5) * _Turbulence;
                sway += turb * height01;

                worldPos.xz += dirXZ * sway;

                OUT.positionWS = worldPos;
                OUT.positionCS = TransformWorldToHClip(worldPos);
                OUT.normalWS = NormalizeNormalPerVertex(normInputs.normalWS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);

                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float4 baseCol = tex * _Color;

                // 顶部稍微更亮，模拟受光的草尖
                float heightMask = saturate(IN.uv.y * 1.2);
                float3 tipped = baseCol.rgb * lerp(1.0, 1.25, heightMask);
                float4 albedo = float4(tipped, baseCol.a);

                clip(albedo.a - _Cutoff);

                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float NdotL = saturate(dot(n, -L));

                float3 diffuse = albedo.rgb * (NdotL * mainLight.color.rgb);
                float3 ambient = albedo.rgb * 0.35;

                float3 color = diffuse + ambient;
                return half4(color, albedo.a);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

