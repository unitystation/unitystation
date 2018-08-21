Shader "PostProcess/Mask Blit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Mask ("Texture", 2D) = "white" {}
		_Alpha ("Alpha", Float) = 1
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
			
			sampler2D _MainTex;
			sampler2D _Mask;
			float _Alpha;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 screen = tex2D(_MainTex, i.uv);
				fixed4 maskSample = tex2D(_Mask, i.uv);

				float mask = 1 - clamp(maskSample.g + maskSample.r, 0, 1);

				return lerp(screen, screen * (1 - _Alpha), mask);
			}
			ENDCG
		}
	}
}
