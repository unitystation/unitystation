// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unitystation/Shader/UIBlending"
{
    Properties {
        _MainTex ("Main Texture", 2D) = "white" {}
        _UITexture ("UI Texture", 2D) = "white" {}
    }

    SubShader {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 position : POSITION;
                half2 texCoord : TEXCOORD0;
            };

            struct v2f {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float alpha : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _UITexture;
            float4 _MainTex_ST;
            float4 _UITexture_ST;

            v2f vert (appdata v) {
                v2f o;
                o.position = UnityObjectToClipPos(v.position);
                o.uv = (v.texCoord - _MainTex_ST.zw) / _MainTex_ST.xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 uiTex = tex2D(_UITexture,i.uv);
                return uiTex;
            }
            ENDCG
        }
    }

    Fallback "Transparent/Cutout/Diffuse"
}