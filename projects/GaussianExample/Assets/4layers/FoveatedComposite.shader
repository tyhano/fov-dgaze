Shader "Unlit/FoveatedCompositeContinuous"
{
    Properties
    {
        _Tex0 ("Layer0", 2D) = "black" {}
        _Tex1 ("Layer1", 2D) = "black" {}
        _Tex2 ("Layer2", 2D) = "black" {}
        _Tex3 ("Layer3", 2D) = "black" {}

        _GazeUV ("GazeUV", Vector) = (0.5,0.5,0,0)

        // 连续层级控制
        _MaxRadius ("Max Radius", Float) = 0.45
        _LevelGamma ("Level Gamma", Float) = 2.0

        // 混合带控制
        _StartBlend ("Start Blend", Range(0,1)) = 0.35
        _BlendWidth ("Blend Width", Range(0.001,1)) = 0.30

        _DebugTint ("Debug Tint (0/1)", Float) = 0
        _TintStrength ("Tint Strength", Range(0,1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _Tex0, _Tex1, _Tex2, _Tex3;
            float4 _GazeUV;

            float _MaxRadius;
            float _LevelGamma;
            float _StartBlend;
            float _BlendWidth;

            float _DebugTint;
            float _TintStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 LayerTint(int layer)
            {
                if (layer == 0) return float3(1, 0, 0);
                if (layer == 1) return float3(0, 1, 0);
                if (layer == 2) return float3(0, 0, 1);
                return float3(1, 1, 0);
            }

            fixed4 SampleLayer(int layer, float2 uv)
            {
                if (layer <= 0) return tex2D(_Tex0, uv);
                if (layer == 1) return tex2D(_Tex1, uv);
                if (layer == 2) return tex2D(_Tex2, uv);
                return tex2D(_Tex3, uv);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float d = distance(uv, _GazeUV.xy);

                // 归一化距离：中心 0，外圈接近 1
                float dn = saturate(d / max(_MaxRadius, 1e-5));

                // 连续 level：0 ~ 3
                // gamma > 1 会让中心高质量区更大一些，类似更“聚焦”
                float level = pow(dn, _LevelGamma) * 4.0;
                level = clamp(level, 0.0, 3.999);

                int baseLevel = (int)floor(level);
                if (baseLevel < 0) baseLevel = 0;
                if (baseLevel > 3) baseLevel = 3;

int nextLevel = min(baseLevel + 1, 3);

                float fracPart = level - (float)baseLevel;

                // 模仿官方逻辑：
                // x = (frac - startBlend) / blendWidth
                float x = (fracPart - _StartBlend) / max(_BlendWidth, 1e-5);
                x = saturate(x);

                // 官方同款风格的平滑函数：3x^2 - 2x^3
                float blendT = 3.0 * x * x - 2.0 * x * x * x;

                float wBase = 1.0 - blendT;
                float wNext = blendT;

                fixed4 cBase = SampleLayer(baseLevel, uv);
                fixed4 cNext = SampleLayer(nextLevel, uv);

                fixed4 col = cBase * wBase + cNext * wNext;

                if (_DebugTint > 0.5)
                {
                    // 调试时给主层上色
                    float3 tint = LayerTint(baseLevel);
                    col.rgb = lerp(col.rgb, tint, _TintStrength);
                }

                return col;
            }
            ENDHLSL
        }
    }
}