// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PostProcess/WallBleed ZoomBlur"
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
	uniform float4 _MainTex_ST;
	uniform float2 _MainTex_TexelSize;

	uniform float _Radius;
	 

	vertexOutput vert (vertexInput IN)
	{
		vertexOutput OUT;

		//OUT.vertex = UnityObjectToClipPos(IN.vertex);
		//OUT.texcoord = IN.uv;

		OUT.vertex = UnityObjectToClipPos(IN.vertex);

		float4 vPos = ComputeScreenPos(IN.vertex);
		OUT.texcoord = IN.uv * float2(0.99f, 0.99f) + float2(0.005f, 0.005f);// (vPos.xy/vPos.w - 0.5) * float2(1,1) + 0.5;

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

		float2 offset = float2(_MainTex_TexelSize.x * _Radius * 1.33333333, 0.0); 

	#if UNITY_VERSION >= 540
		float2 uv = UnityStereoScreenSpaceUVAdjust(IN.uv, _MainTex_ST);
	#else
		float2 uv = IN.uv;
	#endif

		OUT.texcoord = uv;
		OUT.blurTexcoord.xy = uv + offset;
		OUT.blurTexcoord.zw = uv - offset;

		
		//float4 vPos = ComputeScreenPos(IN.vertex);
		//OUT.shifttexcoord = (vPos.xy/vPos.w - 0.5) * float2(1.0f, 1.0f) + 0.5;


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
		 
		
		//float4 vPos = ComputeScreenPos(IN.vertex);
		//OUT.shifttexcoord = (vPos.xy/vPos.w - 0.5) * float2(1.0f, 1.0f) + 0.5;


		return OUT;
	}

	fixed4 frag5Blur (output_5tap IN) : SV_Target
	{
		fixed3 mainSample = tex2D(_MainTex, IN.texcoord * float2(0.99f, 0.99f)).xyz;


		return mainSample.rgbb;
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
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}

		Pass
		{
						CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}
