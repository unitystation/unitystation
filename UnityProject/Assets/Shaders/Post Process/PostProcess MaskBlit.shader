Shader "PostProcess/Mask Blit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 lightUv : TEXCOORD1;
                float2 occlusionUv : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            sampler2D _OcclusionMask;
            sampler2D _ObstacleLightMask;
            sampler2D _LightMask;
            sampler2D _MainTex;
            sampler2D _BackgroundTex;
            sampler2D _ShadowTex;
            sampler2D _ItemTex;
            sampler2D _FullbrightTex;

            float4 _LightTransform;
            float4 _OcclusionTransform;
            float _ShadowAlpha;

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.lightUv = (v.uv - 0.5 + _LightTransform.xy) * _LightTransform.zw + 0.5;
                o.occlusionUv = (v.uv - 0.5 + _OcclusionTransform.xy) * _OcclusionTransform.zw + 0.5;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample masks
                fixed4 occlusionSample = tex2D(_OcclusionMask, i.occlusionUv);
                half4 lightSample = tex2D(_LightMask, i.lightUv);
                fixed4 occLightSample = tex2D(_ObstacleLightMask, i.lightUv);

                half obstacleMask = occlusionSample.r;

                // Composite item over screen
                fixed4 screen = tex2D(_MainTex, i.uv);
                fixed4 item = tex2D(_ItemTex, i.uv);
                half invItemA = 1.0 - item.a;
                screen.rgb = mad(screen.rgb, invItemA, item.rgb);
                screen.a = saturate(mad(screen.a, invItemA, item.a));

                half4 background = tex2D(_BackgroundTex, i.uv);

                // Light mixing
                half4 mixedLight = lightSample * 1.5;
                half mixedA = mixedLight.a;

                half sumRGB = mixedLight.r + mixedLight.g + mixedLight.b;
                half length = sqrt(sumRGB * 2.0);

                half3 normaliseColour = mixedLight.rgb * (2.25 / (length + 0.0001));

                half3 balancedMixLight = clamp(normaliseColour * (mixedA - 0.66), 0.0, 10.0);
                mixedA = mad(mixedA, 1.1, -0.55); // (a - 0.5) * 1.1

                half3 BalanceLight = saturate(normaliseColour * saturate(occLightSample.a + mixedA + 0.55));
                BalanceLight = mad(occLightSample.rgb, 0.75 * obstacleMask, BalanceLight);

                half4 screenLit = half4(mad(screen.rgb, BalanceLight, balancedMixLight), screen.a);

                // Background blend
                half backgroundMask = saturate(occlusionSample.g - screen.a * 2.0);
                half4 screenLitBackground = mad(background, backgroundMask, screenLit);

                // Shadows
                half4 shadowSample = tex2D(_ShadowTex, i.uv);
                half mask_mid = step(0.45, shadowSample.a) * step(shadowSample.a, 0.95);
                half mask_low = step(shadowSample.a, 0.1);
                half shadowMask = mask_mid * max(shadowSample.r, shadowSample.b)
                    + mask_low * (shadowSample.r + shadowSample.g + shadowSample.b);

                screenLitBackground.rgb *= 1.0 - shadowMask * _ShadowAlpha * 2.0;

                // Fullbright overlay
                fixed4 fullbright = tex2D(_FullbrightTex, i.uv);
                fullbright.a *= occlusionSample.g + occlusionSample.r;

                half invFullbrightA = 1.0 - fullbright.a;
                screenLitBackground.rgb = mad(fullbright.rgb, fullbright.a,
                                              screenLitBackground.rgb * invFullbrightA);
                screenLitBackground.a = saturate(mad(screenLitBackground.a, invFullbrightA, fullbright.a));

                return screenLitBackground;
            }
            ENDCG
        }
    }
}