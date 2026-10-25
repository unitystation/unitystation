
Shader "Hidden/Crit State"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "CritState"
            ZTest Always
            Cull Off
            ZWrite Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _CurrentHealth;
            CBUFFER_END

            static const float _GrainSpeed = 0.1;
            static const float vignetteStart = 50.0;
            static const float vignetteEnd = -100.0;
            static const float greyScaleStart = 50.0;
            static const float greyScaleEnd = -15.0;
            static const float redTintStart = 30.0;
            static const float redTintEnd = -60.0;
            static const float darkeningStart = 15.0;
            static const float darkeningEnd = -100.0;

            static const float3 BT709 = float3(0.2126, 0.7152, 0.0722);
            static const float noiseIntensity = 0.04;

            // Supports the descending health thresholds from the original shader.
            float DescendingSmoothstep(float startValue, float endValue, float value)
            {
                return 1.0 - smoothstep(endValue, startValue, value);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                );

                float luminosity = dot(col.rgb, BT709);

                float greyScaleIntensity =
                    DescendingSmoothstep(greyScaleStart, greyScaleEnd, _CurrentHealth);

                float redTintIntensity =
                    DescendingSmoothstep(redTintStart, redTintEnd, _CurrentHealth) * 0.8;

                float darkeningIntensity =
                    DescendingSmoothstep(darkeningStart, darkeningEnd, _CurrentHealth);

                float vignetteIntensity =
                    DescendingSmoothstep(vignetteStart, vignetteEnd, _CurrentHealth);

                float lensRadius = lerp(1.3, 0.4, min(vignetteIntensity, 0.7));
                float softness =
                    (1.0 - step(vignetteStart, _CurrentHealth)) * 0.4;

                // Vignette, adjusted for the screen aspect ratio.
                float2 centeredUV = uv - float2(0.5, 0.5);
                centeredUV.x *= _ScreenParams.x / max(_ScreenParams.y, 1.0);

                float dist = length(centeredUV);
                float vignette = max(
                    0.1,
                    1.0 - smoothstep(
                        lensRadius - max(softness, 0.0001),
                        lensRadius,
                        dist
                    )
                );

                // Animated grain.
                float2 uvTime = uv + frac(_Time.y * _GrainSpeed);
                float noise =
                    (frac(sin(dot(uvTime, float2(12.9898, 78.233)))
                    * 43758.5453) - 0.5) * 2.0
                    * noiseIntensity * greyScaleIntensity;

                // Desaturate.
                float4 outputColour = lerp(
                    col,
                    float4(luminosity, luminosity, luminosity, col.a),
                    greyScaleIntensity
                );

                // Add the red tint.
                float4 redFilter = float4(
                    luminosity,
                    luminosity * (1.0 - redTintIntensity),
                    luminosity * (1.0 - redTintIntensity),
                    col.a
                );

                outputColour = lerp(
                    outputColour,
                    redFilter,
                    redTintIntensity
                );

                // Darken the image as health falls.
                outputColour = lerp(
                    outputColour,
                    float4(0.0, 0.0, 0.0, col.a),
                    min(darkeningIntensity, 0.7)
                );

                // Apply grain and vignette.
                outputColour.rgb += noise;
                outputColour.rgb *= vignette;

                return outputColour;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
