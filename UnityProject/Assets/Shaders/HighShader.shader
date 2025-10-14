Shader "Custom/HighShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}

        // Hue shift
        _HueIntensity("Hue Intensity", Range(-2,2)) = 1.0

        // Plasma
        _PlasmaScale("Plasma Scale", Float) = 2.5
        _PlasmaBlend("Plasma Blend", Range(-2,2)) = 0.5
        _GreenKey("Target Green", Color) = (0.173, 0.5, 0.106, 1)

        // Distortion
        _DistortionAmount("Distortion Amount", Range(-0.06,0.09)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float _HueIntensity;
            float _PlasmaScale;
            float _PlasmaBlend;
            float4 _GreenKey;
            float _DistortionAmount;

            // Phases injected from C#
            float _HuePhase;
            float _PlasmaPhase;
            float _DistortPhase;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Hue rotation
            float3 HueShift(float3 color, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                float3x3 m = {
                    0.213 + c*0.787 - s*0.213, 0.715 - 0.715*c - 0.715*s, 0.072 - 0.072*c + 0.928*s,
                    0.213 - 0.213*c + 0.143*s, 0.715 + 0.285*c + 0.140*s, 0.072 - 0.072*c - 0.283*s,
                    0.213 - 0.213*c - 0.787*s, 0.715 - 0.715*c + 0.715*s, 0.072 + 0.928*c + 0.072*s
                };
                return mul(m, color);
            }

            // Rainbow for plasma
            float3 rainbow(float h)
            {
                h = fmod(fmod(h,1.0)+1.0,1.0);
                float h6=h*6.0;
                float r=clamp(h6-4.0,0,1)+clamp(2.0-h6,0,1);
                float g=(h6<2)?clamp(h6,0,1):clamp(4.0-h6,0,1);
                float b=(h6<4)?clamp(h6-2.0,0,1):clamp(6.0-h6,0,1);
                return float3(r,g,b);
            }

            float3 plasma(float2 fragCoord, float2 resolution, float time)
            {
                float startA=563.0/512.0;
                float startB=233.0/512.0;
                float startC=4325.0/512.0;
                float startD=312556.0/512.0;
                float advanceA=6.34/512.0*18.2;
                float advanceB=4.98/512.0*18.2;
                float advanceC=4.46/512.0*18.2;
                float advanceD=5.72/512.0*18.2;
                float2 uv=fragCoord*_PlasmaScale/resolution;
                float a=startA+time*advanceA;
                float b=startB+time*advanceB;
                float c=startC+time*advanceC;
                float d=startD+time*advanceD;
                float n=sin(a+3.0*uv.x)+sin(b-4.0*uv.x)+sin(c+2.0*uv.y)+sin(d+5.0*uv.y);
                n=fmod((4.0+n)/4.0,1.0);
                return rainbow(n);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 resolution = 1.0 / _MainTex_TexelSize.xy;
                float2 fragCoord = i.uv * resolution;
                float4 col = tex2D(_MainTex, i.uv);

                // 1) Distortion only, no chromatic
                float2 d = float2(
                    sin(_DistortPhase + i.uv.x * 5.0) * _DistortionAmount,
                    cos(_DistortPhase + i.uv.y * 5.0) * _DistortionAmount
                );
                float2 distortedUV = i.uv + d;
                col.rgb = tex2D(_MainTex, distortedUV).rgb;

                // 2) Hue shift
                float3 hueCol = HueShift(col.rgb, _HuePhase);
                col.rgb = lerp(col.rgb, hueCol, _HueIntensity);

                // 3) Plasma overlay
                float greenness = 1.0 - (length(col.rgb - _GreenKey.rgb) / length(float3(1,1,1)));
                float plasmaAlpha = saturate((greenness - 0.7) / 0.2);
                float3 plasmaCol = plasma(fragCoord, resolution, _PlasmaPhase);
                col.rgb = lerp(col.rgb, plasmaCol, plasmaAlpha * _PlasmaBlend);

                return float4(col.rgb, 1.0);
            }

            ENDCG
        }
    }
}