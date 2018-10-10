Shader "PostProcess/PPRT Preview Blit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_OcclusionMask ("Background", 2D) = "black" {}
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
				//float2 uv : TEXCOORD0;
				float2 occlusionUv : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};
			
			sampler2D _OcclusionMask;
			sampler2D _MainTex;
			float4 _OcclusionOffset;
			
			v2f vert (appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				//o.uv =  v.uv;
				o.occlusionUv = ((v.uv + _OcclusionOffset.xy) * _OcclusionOffset.zw) - (_OcclusionOffset.zw - float2(1,1)) * 0.5f;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				//fixed4 screen = tex2D(_MainTex, i.occlusionUv);
				fixed4 color = tex2D(_MainTex, i.occlusionUv);

				return color;
			}
			
			ENDCG
		}
	}
}