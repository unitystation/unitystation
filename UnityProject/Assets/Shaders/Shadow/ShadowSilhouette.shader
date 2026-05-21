Shader "Custom/ShadowSilhouette"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D)          = "white" {}
        _Cutoff  ("Alpha Cutoff",   Range(0, 1)) = 0.1
    }
    
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        ZWrite Off
        Cull   Off
        Blend  SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed     _Cutoff;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos    : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 Colour = tex2D(_MainTex, i.uv);
                 fixed Alpha = Colour.a;
                clip(Alpha - _Cutoff);
                return fixed4(0, 0, 0, Alpha); 
            }
            ENDCG
        }
    }
    
    SubShader
    {
        Tags { "RenderType" = "TransparentSprite" "Queue" = "Transparent" }
        ZWrite Off
        Cull   Off
        Blend  SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed     _Cutoff;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos    : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed alpha = tex2D(_MainTex, i.uv).a;
                clip(alpha - _Cutoff);
                return fixed4(0, 0, 0, alpha); 
            }
            ENDCG
        }
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f    { float4 pos    : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(0, 0, 0, 1);  
            }
            ENDCG
        }
    }
}
