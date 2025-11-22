Shader "Custom/URP/VertexLit_VertexColor_AO_Outline"
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

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0.001,0.08)) = 0.02
        _OutlineMode ("Outline Mode (0=Uniform,1=Darken,2=Blend)", Range(0,2)) = 1
        _OutlineDarken ("Darken Factor", Range(0,1)) = 0.4
        _OutlineBlend ("Blend Factor", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        LOD 300

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

            float4 _OutlineColor;
            float _OutlineThickness;
            float _OutlineMode;
            float _OutlineDarken;
            float _OutlineBlend;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
                float2 quadUV     : TEXCOORD1;
                float2 maskUV     : TEXCOORD2;
                float2 uv3        : TEXCOORD3;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float4 color      : COLOR;
                float ao          : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                float2 quadUV     : TEXCOORD3;
                float edgeMask    : TEXCOORD4;
                float shadow      : TEXCOORD5;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                o.ao = v.maskUV.y;
                o.viewDirWS = normalize(_WorldSpaceCameraPos - TransformObjectToWorld(v.positionOS.xyz));
                o.quadUV = v.quadUV;
                o.edgeMask = v.maskUV.x;
                o.shadow = v.uv3.x;
                return o;
            }

            static float BlinnSpec(float3 N, float3 L, float3 V, float shin)
            {
                float3 H = normalize(L + V);
                float ndh = max(0.0, dot(N, H));
                return pow(ndh, shin);
            }

            static float HasBit(float mask, int b)
            {
                float p = pow(2.0, (float)b);
                float m = floor(mask / p);
                return fmod(m, 2.0) >= 0.5 ? 1.0 : 0.0;
            }

            static float EdgeDistanceFlagged(float2 uv, float mask, float thickness)
            {
                float INF = 1e3;
                float dmin = INF;
                if (HasBit(mask, 0) > 0.5) dmin = min(dmin, uv.y);
                if (HasBit(mask, 1) > 0.5) dmin = min(dmin, 1.0 - uv.x);
                if (HasBit(mask, 2) > 0.5) dmin = min(dmin, 1.0 - uv.y);
                if (HasBit(mask, 3) > 0.5) dmin = min(dmin, uv.x);
                if (dmin == INF) return 1.0;
                return saturate(smoothstep(0.0, thickness, dmin));
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

                float3 litColor = diff * lightCol + ambient + specCol + rimCol;

                float brightness = max(i.color.r, max(i.color.g, i.color.b));
                float emFac = saturate((brightness - _EmissionThreshold) / (1.0 - _EmissionThreshold));
                float3 emission = i.color.rgb * _EmissionStrength * emFac;
                litColor += emission;
                litColor *= i.shadow;
                float interior = EdgeDistanceFlagged(i.quadUV, i.edgeMask, _OutlineThickness);
                float4 baseCol = float4(litColor, i.color.a);

                float4 outlineCol;
                if (abs(_OutlineMode - 1.0) < 0.5)
                {
                    outlineCol = float4(i.color.rgb * _OutlineDarken, i.color.a);
                }
                else if (abs(_OutlineMode - 2.0) < 0.5)
                {
                    outlineCol = lerp(_OutlineColor, i.color, _OutlineBlend);
                }
                else
                {
                    outlineCol = _OutlineColor;
                }

                float4 outCol = lerp(outlineCol, baseCol, interior);
                return half4(outCol);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/BlitCopy"
}
