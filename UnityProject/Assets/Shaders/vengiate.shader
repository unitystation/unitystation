Shader "Unitystation/Shader/Vignette"
{
    Properties
    {
        _MainColor ("Color", Color) = (1,1,1,1)
        _vignetteColor ("Vignette Color", Color) = (1,1,1,1)
        _vignetteSize ("Vignette Size", Float) = 0.0
        _vignetteIntensity ("Vignette Intensity", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Vignette"

            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _vignetteColor;
                float _vignetteSize;
                float _vignetteIntensity;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // Sample the camera image supplied by the URP blit.
                half4 color = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                ) * _MainColor;

                // Preserve the original vignette calculation.
                float2 center = float2(0.5, 0.5);
                float distanceFromCenter = length(uv - center);

                float vignette = smoothstep(
                    _vignetteSize,
                    _vignetteSize - _vignetteIntensity,
                    distanceFromCenter
                );

                color.rgb = lerp(
                    color.rgb,
                    _vignetteColor.rgb,
                    vignette
                );

                return color;
            }

            ENDHLSL
        }
    }
}
