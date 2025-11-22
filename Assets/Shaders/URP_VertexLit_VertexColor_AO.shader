Shader "Custom/URP/VertexLit_VertexColor_AO"
{
    Properties
    {
        _SpecColor ("Specular Color", Color) = (1,1,1,1)
        _SpecStrength ("Specular Strength", Range(0,1)) = 1
        _Shininess ("Shininess", Range(1,256)) = 24
        _RimColor ("Rim Color", Color) = (1,0.6,0.2,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 2
        _AO_Strength ("AO Strength", Range(0,1)) = 1
        _EmissionStrength ("Emission Strength", Range(0,8)) = 1
        _EmissionThreshold ("Emission Threshold", Range(0,1)) = 0.8
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _SpecColor;
            float _SpecStrength;
            float _Shininess;
            float4 _RimColor;
            float _RimPower;
            float _AO_Strength;
            float _EmissionStrength;
            float _EmissionThreshold;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
                float2 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float4 color      : COLOR;
                float ao          : TEXCOORD1;
                float shadow      : TEXCOORD2;
                float3 viewDirWS  : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                o.ao = v.uv2.y;
                o.shadow = v.uv3.x;
                o.viewDirWS = normalize(_WorldSpaceCameraPos - TransformObjectToWorld(v.positionOS.xyz));
                return o;
            }

            static float BlinnSpec(float3 N, float3 L, float3 V, float shin)
            {
                float3 H = normalize(L + V);
                float ndh = max(0.0, dot(N, H));
                return pow(ndh, shin);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 N = normalize(i.normalWS);
                float3 V = normalize(i.viewDirWS);

                Light mainLight = GetMainLight();
                float3 Ldir = -normalize(mainLight.direction);
                float3 lightCol = mainLight.color;

                float NdotL = max(0.0, dot(N, Ldir));
                float3 diff = i.color.rgb * NdotL;

                float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * 0.5;

                float ao = saturate(i.ao);
                float aoMul = lerp(0.4, 1.0, ao);
                diff *= aoMul;
                ambient *= aoMul * _AO_Strength;

                float spec = BlinnSpec(N, Ldir, V, _Shininess);
                float3 specCol = _SpecColor.rgb * spec * _SpecStrength;

                float rim = pow(saturate(1.0 - max(0.0, dot(N, V))), _RimPower);
                float3 rimCol = _RimColor.rgb * rim;

                float3 colorOut = diff * lightCol + ambient + specCol + rimCol;

                float brightness = max(i.color.r, max(i.color.g, i.color.b));
                float emFac = saturate((brightness - _EmissionThreshold) / (1.0 - _EmissionThreshold));
                float3 emission = i.color.rgb * _EmissionStrength * emFac;
                colorOut += emission;
                colorOut *= i.shadow;

                return half4(colorOut, i.color.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/BlitCopy"
}
