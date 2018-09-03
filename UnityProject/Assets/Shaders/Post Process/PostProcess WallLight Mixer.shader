// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PostProcess/WallLight Mixer"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_WallMask ("Texture", 2D) = "white" {}
		_FovExtendedMask ("Texture", 2D) = "white" {}
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
			
			sampler2D _FovExtendedMask;
			sampler2D _MainTex;
			sampler2D _WallMask;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 wallSample = tex2D(_WallMask, i.uv);
				fixed4 lightSample = tex2D(_MainTex, i.uv);
				fixed4 obstacleSample = tex2D(_FovExtendedMask, i.uv);

				return lightSample + wallSample * obstacleSample.r;
			}
			ENDCG
		}
	}
}
