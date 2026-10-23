Shader "Custom/BadEyesightShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurryStrength ("Blur Strength", Int) = 20
        _COLORBLIND_MODE ("Colorblind Mode", Int) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "BadEyesight"

            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                int _BlurryStrength;
                int _COLORBLIND_MODE;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                half4 original = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                );

                // Preserve the original edge-based blur mask.
                float strength = clamp(
                    (length(float2(0.5, 0.5) - uv) * 4.0) - 0.05,
                    0.0,
                    1.0
                );

                float2 texelSize = _BlitTexture_TexelSize.xy;

                int blurSteps = clamp(_BlurryStrength, 1, 30);

                half4 addColour = half4(0, 0, 0, 0);

                // Four directional samples for each blur step.
                // Step zero samples the original pixel four times,
                // matching the structure of the original shader.
                [loop]
                for (int i = 0; i < blurSteps; i++)
                {
                    float offset = (float)i;

                    addColour += SAMPLE_TEXTURE2D_X(
                        _BlitTexture, sampler_LinearClamp,
                        uv + float2(texelSize.x * offset, 0)
                    );

                    addColour += SAMPLE_TEXTURE2D_X(
                        _BlitTexture, sampler_LinearClamp,
                        uv + float2(0, texelSize.y * offset)
                    );

                    addColour += SAMPLE_TEXTURE2D_X(
                        _BlitTexture, sampler_LinearClamp,
                        uv - float2(texelSize.x * offset, 0)
                    );

                    addColour += SAMPLE_TEXTURE2D_X(
                        _BlitTexture, sampler_LinearClamp,
                        uv - float2(0, texelSize.y * offset)
                    );
                }

                half4 blurred = addColour / (4.0 * blurSteps);

                return lerp(original, blurred, strength);
            }
            ENDHLSL
        }
    }
}