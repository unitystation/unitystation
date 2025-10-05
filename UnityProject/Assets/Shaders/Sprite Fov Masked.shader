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
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"
        }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                half2 screencoord : TEXCOORD1;
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


            v2f vert(appdata_t v)
            {
                v2f o;
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

                float maskChannel = maskSample.g + maskSample.r;
                float alphaFactor = clamp(maskChannel * 3 - 0.33333f, 0, 1);
                final.a = textureSample.a * alphaFactor * i.color.a;

                // --- Shadow (only if USE_SHADOW is enabled) ---
                #ifdef USE_SHADOW
                float2 shadowUV = _ShadowOffset.xy;
                shadowUV = float2(shadowUV.x * _MainTex_TexelSize.x, shadowUV.y * _MainTex_TexelSize.y);
                shadowUV += i.texcoord;

                fixed4 shadowSample = tex2D(_MainTex, shadowUV);
                float shadowAlpha = step(0.99, shadowSample.a) * alphaFactor * i.color.a * _ShadowAlpha;

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