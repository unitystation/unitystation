Shader "PostProcess/Mask Blit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always Blend Off

        Pass
        {
            Name "MaskBlit"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Provides Vert (uses SV_VertexID, no mesh needed) and the Varyings struct:
            //   Varyings { float4 positionCS : SV_POSITION; float2 texcoord : TEXCOORD0; }
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            // Samplers are named sampler_<Texture> so each texture keeps the filter mode
            // set from C# (SetFilterMode), exactly like sampler2D did in the old shader.
            TEXTURE2D(_MainTex);          SAMPLER(sampler_MainTex);
            TEXTURE2D(_OcclusionMask);    SAMPLER(sampler_OcclusionMask);
            TEXTURE2D(_ObstacleLightMask);SAMPLER(sampler_ObstacleLightMask);
            TEXTURE2D(_LightMask);        SAMPLER(sampler_LightMask);
            TEXTURE2D(_BackgroundTex);    SAMPLER(sampler_BackgroundTex);
            TEXTURE2D(_ShadowTex);        SAMPLER(sampler_ShadowTex);
            TEXTURE2D(_ItemTex);          SAMPLER(sampler_ItemTex);
            TEXTURE2D(_FullbrightTex);    SAMPLER(sampler_FullbrightTex);

            float4 _LightTransform;
            float4 _OcclusionTransform;
            float _ShadowAlpha;

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                
                float2 lightUv = (uv - 0.5 + _LightTransform.xy) * _LightTransform.zw + 0.5;
                float2 occlusionUv = (uv - 0.5 + _OcclusionTransform.xy) * _OcclusionTransform.zw + 0.5;

                // Sample masks
                half4 occlusionSample = SAMPLE_TEXTURE2D(_OcclusionMask, sampler_OcclusionMask, occlusionUv);
                half4 lightSample = SAMPLE_TEXTURE2D(_LightMask, sampler_LightMask, lightUv);
                half4 occLightSample = SAMPLE_TEXTURE2D(_ObstacleLightMask, sampler_ObstacleLightMask, lightUv);

                half obstacleMask = occlusionSample.r;

                // Composite item over screen
                half4 screen = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 item = SAMPLE_TEXTURE2D(_ItemTex, sampler_ItemTex, uv);
                half invItemA = 1.0 - item.a;
                screen.rgb = mad(screen.rgb, invItemA, item.rgb);
                screen.a = saturate(mad(screen.a, invItemA, item.a));

                half4 background = SAMPLE_TEXTURE2D(_BackgroundTex, sampler_BackgroundTex, uv);

                // Light mixing
                half4 mixedLight = lightSample * 1.5;
                half mixedA = mixedLight.a;

                half sumRGB = mixedLight.r + mixedLight.g + mixedLight.b;
                half len = sqrt(sumRGB * 2.0);

                half3 normaliseColour = mixedLight.rgb * (2.25 / (len + 0.0001));

                half3 balancedMixLight = clamp(normaliseColour * (mixedA - 0.66), 0.0, 10.0);
                mixedA = mad(mixedA, 1.1, -0.55); // (a - 0.5) * 1.1

                half3 BalanceLight = saturate(normaliseColour * saturate(occLightSample.a + mixedA + 0.55));
                BalanceLight = mad(occLightSample.rgb, 0.75 * obstacleMask, BalanceLight);

                half4 screenLit = half4(mad(screen.rgb, BalanceLight, balancedMixLight), screen.a);

                // Background blend
                half backgroundMask = saturate(occlusionSample.g - screen.a * 2.0);
                half4 screenLitBackground = mad(background, backgroundMask, screenLit);

                // Shadows
                half4 shadowSample = SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, uv);
                half mask_mid = step(0.45, shadowSample.a) * step(shadowSample.a, 0.95);
                half mask_low = step(shadowSample.a, 0.1);
                half shadowMask = mask_mid * max(shadowSample.r, shadowSample.b)
                    + mask_low * (shadowSample.r + shadowSample.g + shadowSample.b);

                screenLitBackground.rgb *= 1.0 - shadowMask * _ShadowAlpha * 2.0;

                // Fullbright overlay
                half4 fullbright = SAMPLE_TEXTURE2D(_FullbrightTex, sampler_FullbrightTex, uv);
                fullbright.a *= occlusionSample.g + occlusionSample.r;

                half invFullbrightA = 1.0 - fullbright.a;
                screenLitBackground.rgb = mad(fullbright.rgb, fullbright.a,
                                              screenLitBackground.rgb * invFullbrightA);
                screenLitBackground.a = saturate(mad(screenLitBackground.a, invFullbrightA, fullbright.a));

                return screenLitBackground;
            }
            ENDHLSL
        }
    }
}
