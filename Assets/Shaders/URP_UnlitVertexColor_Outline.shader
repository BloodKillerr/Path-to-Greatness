Shader "Custom/URP/UnlitVertexColor_Outline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0.001,0.08)) = 0.02
        _OutlineMode ("Outline Mode (0=Uniform,1=DarkenVertex,2=Blend)", Float) = 1
        _OutlineDarken ("Darken Factor", Range(0,1)) = 0.4
        _OutlineBlend ("Blend Factor", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        LOD 120

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _OutlineColor;
            float _OutlineThickness;
            float _OutlineMode;
            float _OutlineDarken;
            float _OutlineBlend;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 quadUV : TEXCOORD1;
                float2 uv3    : TEXCOORD3;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 quadUV : TEXCOORD0;
                float shadow : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.quadUV = v.quadUV;
                o.shadow = v.uv3.x;
                return o;
            }

            static float EdgeFactor(float2 uv, float thickness)
            {
                float dx = min(uv.x, 1.0 - uv.x);
                float dy = min(uv.y, 1.0 - uv.y);
                float d = min(dx, dy);
                float t = smoothstep(0.0, thickness, d);
                return saturate(t);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float interior = EdgeFactor(i.quadUV, _OutlineThickness);
                half4 baseCol = half4(i.color.rgb * i.shadow, i.color.a);

                half4 outlineCol;
                if (abs(_OutlineMode - 1.0) < 0.5) outlineCol = half4(baseCol.rgb * _OutlineDarken, baseCol.a);
                else if (abs(_OutlineMode - 2.0) < 0.5) outlineCol = lerp(_OutlineColor, baseCol, _OutlineBlend);
                else outlineCol = _OutlineColor;

                half4 result = lerp(outlineCol, baseCol, interior);
                return result;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/BlitCopy"
}
