Shader "Unlit/DashedLineShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Spacing ("Spacing", float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"  

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            Texture2D _MainTex;
            SamplerState sampler_linear_repeat;
            float4 _MainTex_ST;
            float _Spacing;

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // sample the texture
                float mod_x = fmod(i.uv.x, 1);
                half4 col;
                if(mod_x > _Spacing) {
                    col = half4(1, 1, 1, 0);
                } else {
                    col = _MainTex.Sample(sampler_linear_repeat, i.uv);
                }
                return col;
            }
            ENDHLSL
        }
    }
}
