// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

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
		float4 blurTexcoord[2] : TEXCOORD2;
	};

	uniform sampler2D _MainTex;
	uniform float4 _MainTex_ST;
	uniform float4 _MainTex_TexelSize;

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
		//fixed3 color = tex2D(_MainTex, IN.texcoord);
		return 1;//fixed4(color, 1.0);
	}

	//
	//	Small Kernel
	//
	output_5tap vert5Horizontal (vertexInput IN)
	{
		output_5tap OUT;

		OUT.vertex = UnityObjectToClipPos(IN.vertex);

		half aspect = _MainTex_TexelSize.w / _MainTex_TexelSize.z;
		half _horRadius = aspect * _Radius;
 
		float2 offset1 = float2(_horRadius, 0.0); 
		float2 offset2 = float2(_horRadius, _Radius);

	#if UNITY_VERSION >= 540
		float2 uv = UnityStereoScreenSpaceUVAdjust(IN.uv, _MainTex_ST);
	#else
		float2 uv = IN.uv;
	#endif

		OUT.texcoord = uv;
		OUT.blurTexcoord[0].xy = uv + offset1;
		OUT.blurTexcoord[0].zw = uv - offset1;
		OUT.blurTexcoord[1].xy = uv + offset2;
		OUT.blurTexcoord[1].zw = uv - offset2;

		return OUT;
	}

	output_5tap vert5Vertical (vertexInput IN)
	{
		output_5tap OUT;

		OUT.vertex = UnityObjectToClipPos(IN.vertex);

		half aspect = _MainTex_TexelSize.w / _MainTex_TexelSize.z;
		half _horRadius = aspect * _Radius;

		float2 offset1 = float2(0.0, _Radius); 
		float2 offset2 = float2(-_horRadius, _Radius);

	#if UNITY_VERSION >= 540
		float2 uv = UnityStereoScreenSpaceUVAdjust(IN.uv, _MainTex_ST);
	#else
		float2 uv = IN.uv;
	#endif

		OUT.texcoord = uv;
		OUT.blurTexcoord[0].xy = uv + offset1;
		OUT.blurTexcoord[0].zw = uv - offset1;
		OUT.blurTexcoord[1].xy = uv + offset2;
		OUT.blurTexcoord[1].zw = uv - offset2;

		return OUT;
	}

	fixed4 frag5Blur (output_5tap IN) : SV_Target
	{
		float _samplePower = 0.3;
		
		fixed3 mainSample = tex2D(_MainTex, IN.texcoord); //_MainTex.Sample(sampler_linear_clamp, IN.texcoord).xyz;

		fixed3 blurredSum = mainSample * _samplePower; 
		blurredSum += tex2D(_MainTex, IN.blurTexcoord[0].xy).xyz * _samplePower;
		blurredSum += tex2D(_MainTex, IN.blurTexcoord[0].zw).xyz * _samplePower;
		blurredSum += tex2D(_MainTex, IN.blurTexcoord[1].xy).xyz * _samplePower;
		blurredSum += tex2D(_MainTex, IN.blurTexcoord[1].zw).xyz * _samplePower;

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
