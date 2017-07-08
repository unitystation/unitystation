// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

/*

This is simplest and fastest light shader without path tracking. 
It's works like "Light 1 Point", but ignores obstacles at all.
It requires no aditional data to work so it could be used with Particle System.

*/

Shader "Light2D/Light Ignoring Obstacles" {
Properties {
	_MainTex ("Light texture", 2D) = "white" {}
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
				float2 texcoord : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				fixed4 color : COLOR;
				float4 scrPos  : TEXCOORD2;
			};
			
		    sampler2D _ObstacleTex;
			sampler2D _MainTex;
			half _EmissionColorMul;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;
				o.scrPos = ComputeScreenPos(o.vertex);
				o.color = v.color;
				return o;
			}
			
			fixed4 frag (v2f i) : COLOR
			{
                fixed4 tex = tex2D(_MainTex, i.texcoord);

				fixed4 col = i.color*tex*tex.a*i.color.a;
				col.rgb *= _EmissionColorMul;
                return col;
			}
		ENDCG
	}
}

}