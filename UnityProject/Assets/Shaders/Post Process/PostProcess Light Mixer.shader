// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PostProcess/Light Mixer"
{
	Properties
	{
		_LightMask ("Texture", 2D) = "white" {}
		_ObstacleLightMask ("Texture", 2D) = "white" {}
		_OcclusionMask ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;

			};

			v2f vert (appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv =  v.uv;

				return o;
			}
			
			sampler2D _LightMask;
			sampler2D _ObstacleLightMask;
			sampler2D _OcclusionMask; 

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 lightSample = tex2D(_LightMask, i.uv);
				fixed4 obstacleLightSample = tex2D(_ObstacleLightMask, i.uv);
				fixed4 occlusionSample = tex2D(_OcclusionMask, i.uv);
				


				return lightSample + obstacleLightSample * clamp(occlusionSample.r - 0.5f, 0, 1) * 2;
			}
			ENDCG
		}
	}
}
