Shader "Hidden/Noir Effect"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _GrainSpeed("Grain Speed", Range(0.0, 1.0)) = 0.1
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
            Name "NoirEffect"
            ZTest Always
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _LensRadius;
                float _GrainSpeed;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                half4 col = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                );
                
                col.rgb *= 1.1;

                col.r = abs(col.r);
                col.gb = col.r;

                // Lens vignette.
                float dist = distance(uv, float2(0.5, 0.5)) / 1.1;

                float vignette = 1.0 - smoothstep(
                    _LensRadius - 0.2,
                    _LensRadius,
                    dist
                );

                col.rgb *= vignette;

                // Animated grain.
                float2 uvTime = uv + frac(_Time.y * _GrainSpeed);

                float noise =
                (frac(
                    sin(dot(uvTime, float2(12.9898, 78.233)))
                    * 43758.5453
                ) - 0.5) * 2.0;

                col.rgb += noise * 0.06;

                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}