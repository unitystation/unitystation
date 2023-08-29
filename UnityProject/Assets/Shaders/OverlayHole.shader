Shader "Custom/OverlayHole" {
    Properties {
        _Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Center ("Hole Center", Vector) = (.5, .5, 0 , 0)
        _Radius ("Hole Radius", Float) = .25
        _Shape ("Hole Shape", Float) = .25
        _MainTex ("Main Texture", 2D) = ""
    }
    
    SubShader {
        Tags {"Queue" = "Transparent"}
        Pass {
            ZWrite Off
            ColorMask RGB
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 position : POSITION;
                half2 texCoord : TEXCOORD0;
            };

            struct v2f {
                float4 position_clip : SV_POSITION;
                half2 position_uv : TEXCOORD0;
            };

            uniform half4 _MainTex_ST;

            v2f vert(appdata i) {
                v2f o;
                o.position_clip = UnityObjectToClipPos(i.position);
                o.position_uv = (i.texCoord - _MainTex_ST.zw) / _MainTex_ST.xy;
                return o;
            }

            uniform float4 _Color;
            uniform sampler2D _MainTex;
            uniform half2 _Center;
            half _Radius, _Shape;
            fixed4 frag(v2f i) : SV_Target {
                half2 uv = i.position_uv * _MainTex_ST.xy + _MainTex_ST.zw;
                fixed4 baseColor = tex2D(_MainTex, uv);
                half hole = min(distance(i.position_uv, _Center) / _Radius, 1);
                _Color = _Color * _Color.a;
                _Color *= pow(hole, _Shape) * 0.8;
                
                fixed4 finalColor = ( _Color) + baseColor;
                return finalColor;
            }
            ENDCG
        }
    }
}