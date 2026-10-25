Shader "Custom/Colourblind"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TRITAN ("Tritan", Int) = 0
        _PROTAN ("Protan", Int) = 0
        _DEUNTAN ("Deuntan", Int) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Colourblind"

            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                int _TRITAN;
                int _PROTAN;
                int _DEUNTAN;
            CBUFFER_END

            // Convert RGB to LMS.
            void RGBToLMS(
                float3 colour,
                out float L,
                out float M,
                out float S)
            {
                L = 17.8824 * colour.r
                    + 43.5161 * colour.g
                    + 4.11935 * colour.b;

                M = 3.45565 * colour.r
                    + 27.1554 * colour.g
                    + 3.86714 * colour.b;

                S = 0.0299566 * colour.r
                    + 0.184309 * colour.g
                    + 1.46709 * colour.b;
            }

            // Convert LMS back to RGB using the original coefficients.
            float3 LMSToRGB(float l, float m, float s)
            {
                return float3(
                    0.0809444479 * l
                    - 0.130504409 * m
                    + 0.116721066 * s,

                    -0.0102485335 * l
                    + 0.0540193266 * m
                    - 0.113614708 * s,

                    -0.000365296938 * l
                    - 0.00412161469 * m
                    + 0.693511405 * s
                );
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                half4 colour = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                );

                float L, M, S;
                float l, m, s;

                // Tritan simulation.
                if (_TRITAN == 1)
                {
                    RGBToLMS(colour.rgb, L, M, S);

                    l = L;
                    m = M;
                    s = -0.01224491 * L
                        + 0.07203435 * M;

                    colour.rgb = LMSToRGB(l, m, s);
                }

                // Protan simulation.
                // Applied after Tritan if both are enabled.
                if (_PROTAN == 1)
                {
                    RGBToLMS(colour.rgb, L, M, S);

                    l = 2.27376148 * M
                        - 5.92721645 * S;

                    m = M;
                    s = S;

                    colour.rgb = LMSToRGB(l, m, s);
                }

                // Deuntan simulation.
                // Applied after the previous enabled modes.
                if (_DEUNTAN == 1)
                {
                    RGBToLMS(colour.rgb, L, M, S);

                    l = L;
                    m = 0.494207 * L
                        + 1.24827 * S;

                    s = S;

                    colour.rgb = LMSToRGB(l, m, s);
                }

                return colour;
            }
            ENDHLSL
        }
    }
}