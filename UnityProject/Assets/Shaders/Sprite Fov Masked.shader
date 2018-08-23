﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Unlit alpha-blended shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "Stencil/Unlit background masked" {
	Properties{
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
	}

		SubShader{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 100

		//Stencil{
		//	Ref 1
		//	Comp equal
		//}


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
	};

	struct v2f {
		float4 vertex : SV_POSITION;
		half2 texcoord : TEXCOORD0;
		half2 screencoord : TEXCOORD1;
		UNITY_FOG_COORDS(1)
	};

	sampler2D _MainTex;
	sampler2D _FovMask;
	float4 _MainTex_ST;

	v2f vert(appdata_t v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
		o.screencoord = ComputeScreenPos(o.vertex);
		UNITY_TRANSFER_FOG(o,o.vertex);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{

		fixed4 col = tex2D(_MainTex, i.texcoord);
		fixed4 mask = tex2D(_FovMask, i.screencoord);

		UNITY_APPLY_FOG(i.fogCoord, col);

		float maskChennel = mask.g + mask.r;
		col.a = col.a * maskChennel;

		return col;
	}
		ENDCG
	}
	}

}