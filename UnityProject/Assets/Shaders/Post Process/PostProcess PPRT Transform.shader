Shader "PostProcess/PPRT Transform Blit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_SourceTex ("Texture", 2D) = "white" {}
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
				float2 transformedUv : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};
			
			sampler2D _MainTex;
			sampler2D _SourceTex;
			
			float4 _Transform;
			
			v2f vert (appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.transformedUv = (v.uv - 0.5 + _Transform.xy) * _Transform.zw + 0.5;

				return o;
			} 

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 color = tex2D(_SourceTex, i.transformedUv);

				return color;
			}
			
			ENDCG
		}
	}
}