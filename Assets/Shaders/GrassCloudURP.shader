Shader "URP/Environment/GrassCloud"
{
    Properties
    {
        _LowColor("Low Color", Color) = (0.25, 0.7, 0.25, 1)
        _HighColor("High Color", Color) = (0.6, 0.95, 0.45, 1)
        _SlopeColor("Slope Color", Color) = (0.6, 0.65, 0.4, 1)

        _HeightMin("Height Min", Float) = 0.0
        _HeightMax("Height Max", Float) = 20.0

        _CloudScale("Cloud Scale", Float) = 0.05
        _CloudDetail("Cloud Detail", Float) = 0.15
        _CloudSpeed("Cloud Speed", Float) = 0.4
        _CloudStrength("Cloud Strength", Float) = 0.6

        _WindDirection("Wind Direction XZ", Vector) = (1, 0, 0.35, 0)

        _NoiseScale("Noise Scale", Float) = 0.25
        _NoiseStrength("Noise Strength", Range(0,1)) = 0.2

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
                float  _CloudScale;
                float  _CloudDetail;
                float  _CloudSpeed;
                float  _CloudStrength;
                float4 _WindDirection;
                float  _NoiseScale;
                float  _NoiseStrength;
                float  _Smoothness;
                float  _Metallic;
            CBUFFER_END

            float hash31(float3 p)
            {
                p = frac(p * 0.3183099 + float3(0.1, 0.2, 0.3));
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            // 平滑 2D 噪声：双线性插值，得到椭圆形/有机云朵形状（非方块）
            float smoothNoise2D(float2 u)
            {
                float2 id = floor(u);
                float2 fr = frac(u);
                fr = fr * fr * (3.0 - 2.0 * fr);

                float h00 = hash31(float3(id, 0.0));
                float h10 = hash31(float3(id + float2(1.0, 0.0), 0.0));
                float h01 = hash31(float3(id + float2(0.0, 1.0), 0.0));
                float h11 = hash31(float3(id + float2(1.0, 1.0), 0.0));

                float bx = lerp(h00, h10, fr.x);
                float tx = lerp(h01, h11, fr.x);
                return lerp(bx, tx, fr.y);
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

                // height & slope base color
                float h01 = saturate((wsPos.y - _HeightMin) / max(0.0001, (_HeightMax - _HeightMin)));
                float slope = saturate(dot(n, float3(0,1,0)));

                float3 baseH = lerp(_LowColor.rgb, _HighColor.rgb, h01);
                float3 baseColor = lerp(_SlopeColor.rgb, baseH, slope);

                // wind direction
                float2 dirXZ = normalize(_WindDirection.xz);
                if (all(dirXZ == 0))
                    dirXZ = float2(1, 0);

                // cloud UV moving over ground
                float2 uvCloud = wsPos.xz * _CloudScale;
                float2 windOffset = dirXZ * (_Time.y * _CloudSpeed);
                float2 u = uvCloud + windOffset;

                // 云朵形状：平滑噪声多尺度叠加 → 更真实、椭圆形/有机（非 Minecraft 方块）
                float n1 = smoothNoise2D(u * _CloudDetail);
                float n2 = smoothNoise2D(u * (_CloudDetail * 1.7) + float2(17.3, 9.7));
                float n3 = smoothNoise2D(u * (_CloudDetail * 0.5) + float2(3.1, 25.2));

                float cloud = n1 * 0.5 + n2 * 0.35 + n3 * 0.15;
                cloud = saturate(cloud);
                cloud = smoothstep(0.35, 0.78, cloud);

                // brightness: darker under clouds
                float brightness = lerp(1.05, 0.75, cloud * _CloudStrength);

                // small noise to avoid banding
                float3 noisePos = wsPos * _NoiseScale;
                float nVal = hash31(floor(noisePos));
                nVal = nVal * 2.0 - 1.0;
                brightness += nVal * _NoiseStrength;

                // apply clouds mainly on flatter ground
                float flatness = saturate(pow(slope, 3.0));
                float3 modulated = baseColor * brightness;
                float3 colorGrass = lerp(baseColor, modulated, flatness);

                // simple lighting
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);

                float3 N = normalize(n);
                float NdotL = saturate(dot(N, -L));
                float3 diffuse = colorGrass * (NdotL * mainLight.color.rgb);
                float3 ambient = colorGrass * 0.15;

                float3 finalCol = diffuse + ambient;
                return half4(finalCol, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

