Shader "Custom/UnlitVertexColor_Outline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0.001,0.08)) = 0.02
        _OutlineMode ("Outline Mode (0=Uniform,1=DarkenVertex,2=Blend)", Range(0,2)) = 1
        _OutlineDarken ("Darken Factor (mode1)", Range(0,1)) = 0.4
        _OutlineBlend ("Blend Factor (mode2)", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _OutlineColor;
            float _OutlineThickness;
            float _OutlineMode;
            float _OutlineDarken;
            float _OutlineBlend;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv0    : TEXCOORD0;
                float2 quadUV : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR0;
                float2 quadUV : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.quadUV = v.quadUV;
                return o;
            }
            static float EdgeMask(float2 uv, float thickness)
            {
                float dx = min(uv.x, 1.0 - uv.x);
                float dy = min(uv.y, 1.0 - uv.y);
                float d = min(dx, dy);
                float t = smoothstep(0.0, thickness, d);
                return saturate(t);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float interior = EdgeMask(i.quadUV, _OutlineThickness);
                fixed4 baseCol = i.color;

                fixed4 outlineCol;
                if (abs(_OutlineMode - 1.0) < 0.5)
                {
                    outlineCol = fixed4(baseCol.rgb * _OutlineDarken, baseCol.a);
                }
                else if (abs(_OutlineMode - 2.0) < 0.5)
                {
                    outlineCol = lerp(_OutlineColor, baseCol, _OutlineBlend);
                }
                else
                {
                    outlineCol = _OutlineColor;
                }

                fixed4 outCol = lerp(outlineCol, baseCol, interior);
                return outCol;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
