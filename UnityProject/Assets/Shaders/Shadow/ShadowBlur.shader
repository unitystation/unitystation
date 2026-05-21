Shader "Custom/ShadowBlur"
{
    Properties
    {
        _MainTex              ("Source",                  2D)     = "white" {}
        _Direction            ("Blur Direction",          Vector)  = (1, 0, 0, 0)
        _Spread               ("Blur Spread",             Float)   = 1.0
        _WallFloorOcclusionTex("Wall/Floor Occlusion RT", 2D)     = "black" {}
        _TableOcclusionTex    ("Table Camera RT",         2D)     = "black" {}
        _OcclusionTransform   ("Occlusion UV Transform",  Vector)  = (0, 0, 1, 1)
    }

    SubShader
    {
        ZWrite Off
        ZTest  Always
        Cull   Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;   // auto-filled by Unity: (1/w, 1/h, w, h)
            float4    _Direction;
            float     _Spread;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos    : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                static const int   TAPS   = 9;
                static const float W[9]   = { 0.0323, 0.0606, 0.0985, 0.1360, 0.1613,
                                              0.1360, 0.0985, 0.0606, 0.0323 };
                static const float OFF[9] = { -4, -3, -2, -1, 0, 1, 2, 3, 4 };

                float2 texel = _MainTex_TexelSize.xy * _Spread;
                float2 dir   = _Direction.xy;

                fixed Alpha = tex2D(_MainTex, i.uv).a;
                
                fixed4 col = 0;
                for (int t = 0; t < TAPS; t++)
                    col += tex2D(_MainTex, i.uv + dir * texel * OFF[t]) * W[t];

                col.a = Alpha;
                return col;
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;               
            sampler2D _WallFloorOcclusionTex; 
            sampler2D _TableOcclusionTex;     
            float4    _OcclusionTransform;    

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos    : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                
                fixed rawAlpha = tex2D(_MainTex, i.uv).a;
                
                fixed4 occSample   = tex2D(_WallFloorOcclusionTex, i.uv);
                fixed  isWall      = step(0.01, occSample.r);   
                
                fixed  isTable     = step(0.01, tex2D(_TableOcclusionTex, i.uv).a);

                //return (isTable,isTable,isTable,isTable);
                fixed  isObject    = rawAlpha - isWall;

                isTable = isTable - isWall - isObject;
                //return (isTable,isTable,isTable,isTable);
                fixed rWall   = rawAlpha * isWall;
               
                fixed bObject = rawAlpha * isObject;
                
                fixed typeAlpha = 0.0;
                typeAlpha = isTable  > 0.5 ? 0.5 : typeAlpha;  
                typeAlpha = isObject > 0.5 ?  0.25  : typeAlpha;  
                typeAlpha = isWall   > 0.5 ?  1: typeAlpha; 
                //return fixed4(isWall, isWall, isWall, 1);
                //return fixed4(typeAlpha, typeAlpha, typeAlpha, typeAlpha);
                return fixed4(rWall, isTable, bObject, typeAlpha);
            }
            ENDCG
        }
    }
}
