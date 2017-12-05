// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

/*

Used to create normal map buffer.

*/

Shader "Light2D/Internal/Normal Map Drawer" {
Properties {
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_NormalTex ("Normalmap", 2D) = "bump" {}
}

SubShader {	
	Tags {
		"Queue"="Transparent" 
		"IgnoreProjector"="True" 
		"RenderType"="Transparent"
		"LightObstacle"="True"
	}

	LOD 100
	Blend SrcAlpha OneMinusSrcAlpha
	Cull Off
	ZWrite Off
	Lighting Off

	Pass {  
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				float2 tangent : TANGENT;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 mainTexcoord : TEXCOORD0;
				half2 normalTexcoord : TEXCOORD1;
				half3 utangent : TEXCOORD2;
				half3 vtangent : TEXCOORD3;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
		    sampler2D _NormalTex;
		    float4 _NormalTex_ST;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.mainTexcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.normalTexcoord = TRANSFORM_TEX(v.texcoord, _NormalTex);
				o.utangent = normalize(mul((float3x3)unity_ObjectToWorld, half3(v.tangent.x, v.tangent.y, 0)));
				o.vtangent = normalize(mul((float3x3)unity_ObjectToWorld, half3(v.tangent.y, -v.tangent.x, 0)));
				return o;
			}
			
			fixed4 frag (v2f i) : COLOR
			{
				fixed4 col = tex2D(_MainTex, i.mainTexcoord);
                fixed2 bumpMap = normalize(UnpackNormal(tex2D(_NormalTex, i.normalTexcoord)));
				bumpMap = -bumpMap;
				fixed2 bump = bumpMap.x*i.utangent + bumpMap.y*i.vtangent;
				fixed2 bumpOut = bump*0.5 + 0.5;
                return fixed4(bumpOut, 0, col.a);
			}
		ENDCG
	}
}

}