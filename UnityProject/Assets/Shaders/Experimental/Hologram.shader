Shader "Stencil/Unlit Hologram Masked" {
    Properties{
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
        [PerRendererData] _IsPaletted("Is Paletted", Int) = 0
        [PerRendererData] _PaletteSize("Palette Size", Int) = 8
        _TintColor("Tint Color", Color) = (1,1,1,1)
        _Transparency("Transparency", Range(0.0, 0.5)) = 0.25
        _CutoutThresh("Cutout Threshold", Range(0.0, 1.0)) = 0.2
        _Distance("Distance", Float) = 1
        _Amplitude("Amplitude", Float) = 1
        _Speed("Speed", Float) = 1
        _Amount("Amount", Range(0.0,1.0)) = 1
    }

    SubShader{
        Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                half2 texcoord : TEXCOORD0;
                half2 screencoord : TEXCOORD1;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            sampler2D _ObjectFovMask;
            float4 _ObjectFovMaskTransformation;
            float4 _MainTex_ST;

            float4 _ColorPalette[256];
            int _IsPaletted;
            int _PaletteSize;

            float4 _TintColor;
            float _Transparency;
            float _CutoutThresh;
            float _Distance;
            float _Amplitude;
            float _Speed;
            float _Amount;

            v2f vert(appdata_t v)
            {
                v2f o;
                
                // Apply hologram distortion to the vertices
                v.vertex.x += sin(_Time.y * _Speed * v.vertex.y * _Amplitude) * _Distance * _Amount;
                o.vertex = UnityObjectToClipPos(v.vertex);

                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.screencoord = (ComputeScreenPos(o.vertex) - 0.5 + _ObjectFovMaskTransformation.xy) * _ObjectFovMaskTransformation.zw + 0.5;
                o.color = v.color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the textures
                fixed4 textureSample = tex2D(_MainTex, i.texcoord);
                fixed4 maskSample = tex2D(_ObjectFovMask, i.screencoord);

                fixed4 final;

                // Palette blending or standard texture sampling
                if (_IsPaletted)
                {
                    int paletteIndexA = floor(textureSample.r * (_PaletteSize-1));
                    int paletteIndexB = floor(textureSample.g * (_PaletteSize-1));
                    final = lerp(_ColorPalette[paletteIndexA], _ColorPalette[paletteIndexB], textureSample.b) * i.color;
                }
                else
                {
                    final = textureSample * i.color;
                }

                // Hologram tint and transparency
                final.rgb += _TintColor.rgb;
                final.a = _Transparency;

                // Mask blending and alpha cutout based on mask and threshold
                float maskChannel = maskSample.g + maskSample.r;
                final.a *= textureSample.a * clamp(maskChannel * 3 - 0.33333f, 0, 1) * i.color.a;
                clip(final.r - _CutoutThresh);

                return final;
            }
            ENDCG
        }
    }
}
