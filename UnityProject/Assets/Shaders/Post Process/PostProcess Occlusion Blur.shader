﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PostProcess/Occlusion Blur"
{
	Properties
	{
		_MainTex ("", 2D) = "white" {}
	}

	CGINCLUDE

	#include "UnityCG.cginc"

	struct vertexInput
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct vertexOutput
	{
		float4 vertex : SV_POSITION;
		float2 texcoord : TEXCOORD0;
	};

	struct output_5tap
	{
		float4 vertex : SV_POSITION;
		float2 texcoord : TEXCOORD0;
		float2 shifttexcoord : TEXCOORD1;
		float4 blurTexcoord : TEXCOORD2;
	};


	uniform sampler2D _MainTex;
	uniform sampler2D _FovMask;
	uniform float4 _MainTex_ST;
	uniform float2 _MainTex_TexelSize;

	uniform float _Radius;
	float2 _MultiLimit;

	vertexOutput vert (vertexInput IN)
	{
		vertexOutput OUT;

		OUT.vertex = UnityObjectToClipPos(IN.vertex);
		OUT.texcoord = IN.uv;

		return OUT;
	}

	fixed4 frag (vertexOutput IN) : SV_Target
	{
		fixed3 color = tex2D(_MainTex, IN.texcoord);
		return fixed4(color, 1.0);
	}

	//
	//	Small Kernel
	//
	output_5tap vert5Horizontal (vertexInput IN)
	{
		output_5tap OUT;

		OUT.vertex = UnityObjectToClipPos(IN.vertex);

		float2 offset = float2(_MainTex_TexelSize.x * _Radius * 0.5f, 0.0); 

	#if UNITY_VERSION >= 540
		float2 uv = UnityStereoScreenSpaceUVAdjust(IN.uv, _MainTex_ST);
	#else
		float2 uv = IN.uv;
	#endif

		OUT.texcoord = uv;
		OUT.blurTexcoord.xy = uv + offset;
		OUT.blurTexcoord.zw = uv - offset;

		return OUT;
	}

	output_5tap vert5Vertical (vertexInput IN)
	{
		output_5tap OUT;

		OUT.vertex = UnityObjectToClipPos(IN.vertex);

		float2 offset = float2(0.0, _MainTex_TexelSize.y * _Radius * 1.33333333); 

	#if UNITY_VERSION >= 540
		float2 uv = UnityStereoScreenSpaceUVAdjust(IN.uv, _MainTex_ST);
	#else
		float2 uv = IN.uv;
	#endif

		OUT.texcoord = uv;
		OUT.blurTexcoord.xy = uv + offset;
		OUT.blurTexcoord.zw = uv - offset;

		return OUT;
	}

	fixed4 frag5Blur (output_5tap IN) : SV_Target
	{
		fixed3 mainSample = tex2D(_MainTex, IN.texcoord).xyz;

		fixed3 blurredSum = mainSample * 0.29411764; 
		blurredSum += tex2D(_MainTex, IN.blurTexcoord.xy).xyz * 0.35294117;
		blurredSum += tex2D(_MainTex, IN.blurTexcoord.zw).xyz * 0.35294117;

		float power = _MultiLimit.x;
		float limit = _MultiLimit.y;

		return clamp(blurredSum.rgb * power, float3(0,0,0), float3(limit, limit, limit)).rgbb;
	}

	ENDCG

	SubShader
	{
		ZTest Always Cull Off ZWrite Off

		//
		//	dummy pass
		//
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}

		//
		//	5 tap gaussian blur
		//
		Pass
		{
			CGPROGRAM
			#pragma multi_compile _ GAMMA_CORRECTION
			#pragma vertex vert5Horizontal
			#pragma fragment frag5Blur
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma multi_compile _ GAMMA_CORRECTION
			#pragma vertex vert5Vertical
			#pragma fragment frag5Blur
			ENDCG
		}
	}
}
