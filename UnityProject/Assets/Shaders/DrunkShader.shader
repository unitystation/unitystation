
Shader "Custom/DrunkShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _LightAmount ("Light Amount", Range(0, 10)) = 5.0
        _CosAmount ("Vertical Pan Amount", Range(0, 0.1)) = 0.05
        _SinAmount ("Horizontal Pan Amount", Range(0, 0.1)) = 0.05
        _Waves ("Waves", Range(0, 0.5)) = 0.25
        _Speed ("Speed", Range(0, 1)) = 0.5
        _DoubleVision ("Double Vision", Range(0, 0.02)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Drunk"

            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _LightAmount;
                float _CosAmount;
                float _SinAmount;
                float _Waves;
                float _Speed;
                float _DoubleVision;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                uv.x += cos(uv.y * _Waves + _Time.y) * _CosAmount;
                uv.y += sin(uv.x * _Waves + _Time.y) * _SinAmount;

                float offset = sin(_Time.y * _Speed) * _DoubleVision;

                half4 a = SAMPLE_TEXTURE2D_X(
                    _BlitTexture, sampler_LinearClamp, uv);

                half4 b = SAMPLE_TEXTURE2D_X(
                    _BlitTexture, sampler_LinearClamp,
                    uv - float2(sin(offset), 0.0));

                half4 c = SAMPLE_TEXTURE2D_X(
                    _BlitTexture, sampler_LinearClamp,
                    uv + float2(sin(offset), 0.0));

                half4 d = SAMPLE_TEXTURE2D_X(
                    _BlitTexture, sampler_LinearClamp,
                    uv - float2(0.0, sin(offset)));

                half4 e = SAMPLE_TEXTURE2D_X(
                    _BlitTexture, sampler_LinearClamp,
                    uv + float2(0.0, sin(offset)));

                // Preserve the original light scaling behavior.
                return (a + b + c + d + e) / max(_LightAmount, 0.0001);
            }

            ENDHLSL
        }
    }
}
