// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Unlit alpha-blended shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "Stencil/Unlit background masked emission" {
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
		Blend SrcAlpha One

		Pass{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fog

#include "UnityCG.cginc"

	struct appdata_t 
	{
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
		float4 color : COLOR;
	};

	struct v2f {
		float4 vertex : SV_POSITION;
		half2 texcoord : TEXCOORD0;
		half2 screencoord : TEXCOORD1;
		float4 color : COLOR;
	};

	sampler2D _MainTex;
    //holds the Fov mask used for object sprites
	sampler2D _ObjectFovMask;
    //holds a vector used to offset the above texture (which is a PPRT) from the renderer. Calculated from objectOcclusionMask.GetTransformation(currentCamera)
	float4 _ObjectFovMaskTransformation;
	float4 _MainTex_ST;

	v2f vert(appdata_t v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

		o.screencoord = (ComputeScreenPos(o.vertex) - 0.5 + _ObjectFovMaskTransformation.xy) * _ObjectFovMaskTransformation.zw + 0.5;
		o.color = v.color;

		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		fixed4 textureSample = tex2D(_MainTex, i.texcoord);
		fixed4 maskSample = tex2D(_ObjectFovMask, i.screencoord);

		float intencity = i.color.r;
		float additionalEmission = i.color.g;
		fixed4 final = (textureSample * intencity);

		float maskChennel =  maskSample.r;

		//0.70 to Because 1 = blown out
		//additionalEmission+1 , Because it goes between 0 and 1, and it should be default on its 0
		final.a = textureSample.a * (additionalEmission+1) * 0.70 * maskChennel ;
		
		return final;
	}
		ENDCG
	}
	}

}