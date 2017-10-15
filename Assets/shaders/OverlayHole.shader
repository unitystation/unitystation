// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

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
         Blend SrcAlpha OneMinusSrcAlpha
         Pass {
             CGPROGRAM
             #pragma vertex vert
             #pragma fragment frag

             #include "UnityCG.cginc"

             struct appdata {
                 float4 position : POSITION;
                 half2 texCoord : TEXCOORD;
             }; 
             
             struct v2f {
                 float4 position_clip : SV_POSITION;
                 half2 position_uv : TEXCOORD;
             };
             

             uniform half4 _MainTex_ST;

             v2f vert(appdata i) {
                 v2f o;
                 o.position_clip = UnityObjectToClipPos(i.position);
                 //UnityObjectToClipPos(i.position);
                 o.position_uv = _MainTex_ST.xy * i.texCoord + _MainTex_ST.zw;
                 //_MainTex_ST.xy * i.texCoord + _MainTex_ST.zw;
                 return o;
             }
             
             uniform float4 _Color;
             uniform sampler2D _MainTex;
             uniform half2 _Center;
             half _Radius, _Shape;
             fixed4 frag(v2f i) : COLOR {        
                 fixed4 fragColor = tex2D(_MainTex, i.position_uv);
                 half hole = min(distance(i.position_uv, _Center) / _Radius, 1);
                 fragColor.a *= pow(hole, _Shape);
                 return _Color * fragColor;
             }
             ENDCG
         }
     }
 }
