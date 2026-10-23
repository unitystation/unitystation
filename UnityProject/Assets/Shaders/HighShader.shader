Shader "Custom/HighShader"
{
    Properties
    {
        // Hue shift
        _HueIntensity("Hue Intensity", Range(-2,2)) = 1.0

        // Plasma
        _PlasmaScale("Plasma Scale", Float) = 2.5
        _PlasmaBlend("Plasma Blend", Range(-2,2)) = 0.5
        _GreenKey("Target Green", Color) = (0.173, 0.5, 0.106, 1)

        // Distortion
        _DistortionAmount("Distortion Amount", Range(-0.06,0.09)) = 0.05

        // Phases injected from C# (declared so they are valid material properties)
        [HideInInspector] _HuePhase("Hue Phase", Float) = 0
        [HideInInspector] _PlasmaPhase("Plasma Phase", Float) = 0
        [HideInInspector] _DistortPhase("Distort Phase", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "HighEffect"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _HueIntensity;
            float _PlasmaScale;
            float _PlasmaBlend;
            float4 _GreenKey;
            float _DistortionAmount;
            float _HuePhase;
            float _PlasmaPhase;
            float _DistortPhase;

            // Hue rotation
            float3 HueShift(float3 color, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                float3x3 m = float3x3(
                    0.213 + c*0.787 - s*0.213, 0.715 - 0.715*c - 0.715*s, 0.072 - 0.072*c + 0.928*s,
                    0.213 - 0.213*c + 0.143*s, 0.715 + 0.285*c + 0.140*s, 0.072 - 0.072*c - 0.283*s,
                    0.213 - 0.213*c - 0.787*s, 0.715 - 0.715*c + 0.715*s, 0.072 + 0.928*c + 0.072*s
                );
                return mul(m, color);
            }

            // Rainbow for plasma
            float3 rainbow(float h)
            {
                h = fmod(fmod(h, 1.0) + 1.0, 1.0);
                float h6 = h * 6.0;
                float r = clamp(h6 - 4.0, 0, 1) + clamp(2.0 - h6, 0, 1);
                float g = (h6 < 2) ? clamp(h6, 0, 1) : clamp(4.0 - h6, 0, 1);
                float b = (h6 < 4) ? clamp(h6 - 2.0, 0, 1) : clamp(6.0 - h6, 0, 1);
                return float3(r, g, b);
            }

            float3 plasma(float2 fragCoord, float2 resolution, float time)
            {
                float startA = 563.0 / 512.0;
                float startB = 233.0 / 512.0;
                float startC = 4325.0 / 512.0;
                float startD = 312556.0 / 512.0;
                float advanceA = 6.34 / 512.0 * 18.2;
                float advanceB = 4.98 / 512.0 * 18.2;
                float advanceC = 4.46 / 512.0 * 18.2;
                float advanceD = 5.72 / 512.0 * 18.2;
                float2 uv = fragCoord * _PlasmaScale / resolution;
                float a = startA + time * advanceA;
                float b = startB + time * advanceB;
                float c = startC + time * advanceC;
                float d = startD + time * advanceD;
                float n = sin(a + 3.0 * uv.x) + sin(b - 4.0 * uv.x) + sin(c + 2.0 * uv.y) + sin(d + 5.0 * uv.y);
                n = fmod((4.0 + n) / 4.0, 1.0);
                return rainbow(n);
            }

            float4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 uv = i.texcoord;
                float2 resolution = 1.0 / _BlitTexture_TexelSize.xy;
                float2 fragCoord = uv * resolution;

                // 1) Distortion only, no chromatic
                float2 d = float2(
                    sin(_DistortPhase + uv.x * 5.0) * _DistortionAmount,
                    cos(_DistortPhase + uv.y * 5.0) * _DistortionAmount
                );
                float3 rgb = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + d).rgb;

                // 2) Hue shift
                float3 hueCol = HueShift(rgb, _HuePhase);
                rgb = lerp(rgb, hueCol, _HueIntensity);

                // 3) Plasma overlay
                float greenness = 1.0 - (length(rgb - _GreenKey.rgb) / length(float3(1, 1, 1)));
                float plasmaAlpha = saturate((greenness - 0.7) / 0.2);
                float3 plasmaCol = plasma(fragCoord, resolution, _PlasmaPhase);
                rgb = lerp(rgb, plasmaCol, plasmaAlpha * _PlasmaBlend);

                return float4(rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
