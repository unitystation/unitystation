// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

/*

This is simplest and fastest light shader without path tracking. 
It requires no aditional data to work so it could be used with Particle System.

*/

Shader "Light2D/Light 1 Point" {
Properties {
	_MainTex ("Light texture", 2D) = "white" {}
	_ObstacleMul ("Obstacle Mul", Float) = 6
	_EmissionColorMul ("Emission color mul", Float) = 1
}
SubShader {	
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}

	LOD 100
	Blend OneMinusDstColor One
	Cull Off
	ZWrite Off
	Lighting Off

	Pass {  
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile ORTHOGRAPHIC_CAMERA PERSPECTIVE_CAMERA
			#pragma multi_compile LIGHT2D_XY_PLANE LIGHT2D_XZ_PLANE
			
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half4 texcoord : TEXCOORD0;
				half4 scrPos  : TEXCOORD1;
				fixed4 color : COLOR0;
			};
			
		    uniform sampler2D _ObstacleTex;
			uniform sampler2D _MainTex;
			uniform half _ObstacleMul;
			uniform half _EmissionColorMul;
			uniform float2 _ExtendedToSmallTextureScale;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;
				o.scrPos = ComputeScreenPos(o.vertex);
				o.scrPos = half4((o.scrPos.xy - 0.5)*_ExtendedToSmallTextureScale + 0.5, o.scrPos.zw);
				o.color = v.color;
				return o;
			}
			
			half4 frag (v2f i) : COLOR
			{
				half2 thisPos = (i.scrPos.xy/i.scrPos.w); 
				half m = _ObstacleMul;
                half4 tex = tex2D(_MainTex, i.texcoord.xy);
				half4 col = i.color*tex*tex.a*i.color.a;
				half4 obstacle = tex2D(_ObstacleTex, thisPos);
				col *= saturate(1 - (1 - obstacle)*obstacle.a*m);
				col.rgb *= _EmissionColorMul;
                return col;
			}
		ENDCG
	}
}

}