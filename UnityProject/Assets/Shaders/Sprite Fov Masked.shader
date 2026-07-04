// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
// Unlit alpha-blended shader.
// - no lighting
// - no lightmap support
// - no per-material color
Shader "Stencil/Unlit background masked"
{
    Properties
    {
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
        [PerRendererData] _IsPaletted("Is Paletted", Int) = 0
        [PerRendererData] _PaletteSize("Palette Size", Int) = 8

        _ShadowColor("Shadow Color", Color) = (0,0,0,0.5)
        _ShadowOffset("Shadow Offset (X,Y)", Vector) = (0.01, -0.01, 0, 0)
        _ShadowAlpha("Shadow Strength", Range(0,1)) = 0.5

        [Toggle(USE_SHADOW)] _UseShadow ("Enable Shadow", Float) = 0

        // Deep fry effect, set per-instance via MaterialPropertyBlock
        [PerRendererData] _FryIntensity("Fry Intensity", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"
        }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

   // IMPORTANT: goes here, not inside frag
        #pragma multi_compile _ USE_SHADOW
            
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half2 texcoord : TEXCOORD0;
                float2 spritecoord : TEXCOORD1;
                half2 screencoord : TEXCOORD2;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            //holds the Fov mask used for object sprites
            sampler2D _ObjectFovMask;
            //holds a vector used to offset the above texture (which is a PPRT) from the renderer. Calculated from objectOcclusionMask.GetTransformation(currentCamera)
            float4 _ObjectFovMaskTransformation;
            float4 _MainTex_ST;

            float4 _ColorPalette[256];
            int _IsPaletted;
            int _PaletteSize;

            float4 _ShadowColor;
            float4 _ShadowOffset;
            float _ShadowAlpha;

            float4 _MainTex_TexelSize;

            float _FryIntensity;


            v2f vert(appdata_t v)
            {
                v2f o;
                o.spritecoord = v.vertex.xy;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.screencoord = (ComputeScreenPos(o.vertex) - 0.5 + _ObjectFovMaskTransformation.xy) *
                    _ObjectFovMaskTransformation.zw + 0.5;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 textureSample = tex2D(_MainTex, i.texcoord);
                fixed4 maskSample = tex2D(_ObjectFovMask, i.screencoord);

                // --- Base color (palette or not) ---
                fixed4 final;
                if (_IsPaletted)
                {
                    int paletteIndexA = floor(textureSample.r * (_PaletteSize - 1));
                    int paletteIndexB = floor(textureSample.g * (_PaletteSize - 1));
                    final = lerp(_ColorPalette[paletteIndexA], _ColorPalette[paletteIndexB], textureSample.b) * i.color;
                }
                else
                {
                    final = textureSample * i.color;
                }

                // --- Deep fry effect (per-instance, controlled via MaterialPropertyBlock) ---
                if (_FryIntensity > 0 && textureSample.a >= 0.5)
                {
                    // === TINT: single flat color per step, covers entire sprite ===
                    float3 tint_color;
                    float tint_opacity;
                    // Thresholds match FriedLevelToIntensity in DeepFryable.cs:
                    // LightlyFried = 0.25, Fried = 0.5, DeepFried = 0.75, TheVeryConcept = 1.0
                    if (_FryIntensity <= 0.25)
                    {
                        // LightlyFried
                        tint_color = float3(0.55, 0.40, 0.22);
                        tint_opacity = 0.6;
                    }
                    else if (_FryIntensity <= 0.5)
                    {
                        // Fried
                        tint_color = float3(0.40, 0.28, 0.14);
                        tint_opacity = 0.72;
                    }
                    else if (_FryIntensity <= 0.75)
                    {
                        // DeepFried
                        tint_color = float3(0.25, 0.17, 0.08);
                        tint_opacity = 0.85;
                    }
                    else
                    {
                        // TheVeryConcept
                        tint_color = float3(0.025, 0.015, 0.008);
                        tint_opacity = 1;
                    }

                    // Tint: scale tint by sprite luminance to preserve detail without
                    // normalising away darkness differences between fry levels.
                    float luma = dot(final.rgb, float3(0.299, 0.587, 0.114));
                    float3 tinted = tint_color * (1.0 + luma);
                    tinted = min(tinted, 1.0);
                    final.rgb = lerp(final.rgb, tinted, tint_opacity);

                    // === NOISE TEXTURE ===
                    float2 block_uv = floor(i.texcoord * 8.0);
                    // Per-block random: determines visibility and brightness
                    float block_rand = frac(sin(dot(block_uv, float2(41.231, 67.892))) * 28461.137);
                    float block_bright = frac(sin(dot(block_uv, float2(73.156, 12.843))) * 53182.947);

                    // Sparsity: fewer blocks at low intensity, more at high, never fully uniform
                    float noise_threshold = max(0.15, 1.0 - _FryIntensity * 1.8);
                    if (block_rand > noise_threshold)
                    {
                        // Block color: brighter or darker variant of the tint
                        float3 noise_bright = tint_color * 1.5;
                        float3 noise_dark = tint_color * 0.4;
                        float3 noise_color = lerp(noise_dark, noise_bright, block_bright);

                        // Additive overlay on top of the tinted result, strength scales with intensity
                        float noise_strength = _FryIntensity * 0.5;
                        final.rgb = lerp(final.rgb, noise_color, noise_strength);
                    }
                }

                float maskChannel = maskSample.g + maskSample.r;
                //float alphaFactor = clamp(maskChannel * 3 - 0.33333f, 0, 1);
                float alphaFactor = 1; // test
                final.a = textureSample.a * alphaFactor * i.color.a;

                
                // --- Shadow (only if USE_SHADOW is enabled) ---
                #ifdef USE_SHADOW
                float2 shadowUV = _ShadowOffset.xy;
                shadowUV = float2(shadowUV.x * _MainTex_TexelSize.x, shadowUV.y * _MainTex_TexelSize.y);
                shadowUV += i.texcoord;

                fixed4 shadowSample = tex2D(_MainTex, shadowUV);
                float shadowAlpha = step(0.99, shadowSample.a) * alphaFactor * i.color.a * _ShadowAlpha;

                
                float2 fraccoord = (i.spritecoord.xy % float2(1,1)) + float2(0.5f,0.5f);
                if(abs(i.spritecoord.x) > 1 || abs(i.spritecoord.y) > 1)
                {
                    fraccoord = ((i.spritecoord.xy + float2(0.5f,0.5f)) % float2(1,1));
                    //Tile maps are centered on 0.5f, 0.5f not 0,0 and also extend beyond 1,1. So we test and correct here
                }

                float2 shadowDirection = sign(_ShadowOffset) * float2(1, 1);
                float2 edgeThresholds =  ((0.99 * shadowDirection) + float2(1.0f,1.0f)) / float2(2.0f, 2.0f);
                //Turns -1 into 0.005 and +1 into 0.995
                if((shadowDirection.x < 0 && fraccoord.x <= edgeThresholds.x) || (shadowDirection.x > 0 && fraccoord.x > edgeThresholds.x)
                || (shadowDirection.y < 0 && fraccoord.y <= edgeThresholds.y) || (shadowDirection.y > 0 && fraccoord.y > edgeThresholds.y))
                {
                    shadowAlpha = 0;
                }
               
                    
                fixed4 shadowColor = _ShadowColor;
                shadowColor.a *= shadowAlpha;
                
                // First draw shadow
                fixed4 output = shadowColor;
                // Then overlay sprite
                output.rgb = lerp(output.rgb, final.rgb, step(0.1, final.a));
                output.a = max(output.a, final.a);
                return output;
                #else
                // Just return the sprite, no shadow
                return final;
                #endif
            }
            ENDCG
        }
    }
}