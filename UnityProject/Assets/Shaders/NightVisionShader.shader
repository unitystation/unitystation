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

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                col = max(0.1, col);
                col *= 2.5;

                col.rgb = abs(col.rgb);
                col.g = col.r;
                col.b = col.r;

                float dist = distance(uv, float2(0.5, 0.5));
                float vignette = 1.0 - smoothstep(_LensRadius - 0.2, _LensRadius, dist / 1.1);
                col.rgb *= vignette;

                float2 screenUV = float2(
                    uv.x * 0.2,
                    uv.y * 0.2 + fmod(_Time.y / 100.0, 0.25)
                );
                half4 screen = SAMPLE_TEXTURE2D(_ScreenTexture, sampler_ScreenTexture, screenUV);
                col.rgb *= _Color.rgb * screen.rgb;

                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
