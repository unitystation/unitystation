Shader "Hidden/Night Vision"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _ScreenTexture("Texture", 2D) = "white" {}
        _Color("Some Color", Color) = (1, 1, 1, 1)
        _LensRadius("Lens Radius", Float) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "NightVision"
            ZTest Always
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_ScreenTexture);
            SAMPLER(sampler_ScreenTexture);

            CBUFFER_START(UnityPerMaterial)
                float _LensRadius;
                float4 _Color;
            CBUFFER_END

            static const float3 BT709 = float3(0.2126, 0.7152, 0.0722);
            static const float VIGNETTE_CORRECTION = 1.1;
            static const float NV_LUMA_BOOST = 2.5;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float luminosity = dot(col.rgb, BT709);
                col.rgb = luminosity * _Color.rgb * NV_LUMA_BOOST;

                float dist = distance(uv, float2(0.5, 0.5));
                float vignette = 1.0 - smoothstep(_LensRadius - 0.2, _LensRadius, dist / 1.1);

                col.rgb *= vignette * VIGNETTE_CORRECTION;

                float2 screenUV = float2(
                    uv.x * 0.2,
                    uv.y * 0.2 + fmod(_Time.y / 100.0, 0.25)
                );
                half4 screen = SAMPLE_TEXTURE2D(_ScreenTexture, sampler_ScreenTexture, screenUV);
                col.rgb *= screen.rgb;

                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
