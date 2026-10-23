Shader "Custom/Fov Mask"
{
    // Special material used to render Fov mask.
    // It has two modes that are controlled by _FovBlurSwitch.
    // First, when _FovBlurSwitch is 0, it renders objects into the R channel
    // (used as an override material by OcclusionMaskFeature).
    // Second, when _FovBlurSwitch is 1, it is used by the FOV mask renderer to render into the G channel
    // (used with Graphics.Blit, which draws a normal mesh quad, so the vertex stage stays mesh based).
    Properties
    {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _FovBlurSwitch ("FovBlurSwitch", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+1000" "IgnoreProjector" = "True" "RenderType" = "Transparent"
        }
        LOD 100

        ZWrite Off
        ZTest Always
        Cull Off

        // Note: Additive blending will add colors together.
        Blend One One

        Pass
        {
            Name "FovMask"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half2 texcoord : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            float _FovBlurSwitch;
            float _DemaskSwitch;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);

                return float4((1 - _FovBlurSwitch) * (1 - _DemaskSwitch) * col.a * col.r * 1000, _FovBlurSwitch, 0, 0);
            }
            ENDHLSL
        }
    }
}