Shader "Hidden/Vampire Vision"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
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
            Name "VampireVision"
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
            CBUFFER_END

            static const float3 BT709 = float3(0.2126, 0.7152, 0.0722);

            static const float _RedThreshold = 0.3;
            static const float _RedSoftness = 0.05;
            static const float _RedDominance = 0.35;
            static const float _HueClamp = 1.0;
            static const float _HueRange = 0.2;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                half4 col = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                );

                float redness = col.r - max(col.g, col.b);
                float saturation = col.r - min(col.g, col.b);

                // Identify strongly red pixels.
                float rMask = smoothstep(
                    _RedThreshold - _RedSoftness,
                    _RedThreshold + _RedSoftness,
                    redness
                );

                rMask *= smoothstep(0.0, _RedDominance, col.r);
                rMask *= smoothstep(0.1, 0.4, saturation);

                // Reduce green and blue in red hues.
                float nearRed = smoothstep(0.0, _HueRange, redness);

                float3 clampedHue = float3(
                    col.r,
                    col.g * (1.0 - _HueClamp * nearRed),
                    col.b * (1.0 - _HueClamp * nearRed)
                );

                // Preserve red hues; desaturate other colors.
                float luminosity = dot(clampedHue, BT709);
                float3 outputColour = lerp(
                    luminosity.xxx,
                    clampedHue,
                    rMask
                );

                // Lens vignette.
                float dist = distance(uv, float2(0.5, 0.5)) / 1.1;

                float vignette = 1.0 - smoothstep(
                    _LensRadius - 0.2,
                    _LensRadius,
                    dist
                );

                outputColour *= vignette;

                return half4(outputColour, col.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}