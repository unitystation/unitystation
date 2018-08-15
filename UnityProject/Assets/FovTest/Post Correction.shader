Shader "Custom/FovMaskCorrect"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_CorrectionOffset ("Correction Offset", Float) = 0
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
				float4 blurUv[2] : TEXCOORD1;
			};

			sampler2D _MainTex;
			float _CorrectionOffset;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);

				float2 offset1 = float2(_CorrectionOffset, _CorrectionOffset);  // * 0.562f
				float2 offset2 = float2(-_CorrectionOffset, _CorrectionOffset); 

				o.blurUv[0].xy = v.uv + offset1;
				o.blurUv[0].zw = v.uv - offset1;
				o.blurUv[1].xy = v.uv + offset2;
				o.blurUv[1].zw = v.uv - offset2;

				o.uv =  v.uv;

				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed3 sum = tex2D(_MainTex, i.uv.xy).xyz; 

				sum.r *= tex2D(_MainTex, i.blurUv[0].xy).r;
				sum.r *= tex2D(_MainTex, i.blurUv[0].zw).r;
				sum.r *= tex2D(_MainTex, i.blurUv[1].xy).r;
				sum.r *= tex2D(_MainTex, i.blurUv[1].zw).r;

				// Create blurr mask.
				// Two stencils each rendered with 0.5f green channel and added toether. Overlapping zone will result in 1 green, and exclusion is 0.5.
				// We use excluded (0.5 green) zone as a mask to detect fov edges.
				
				float _blurMask = (sum.g - clamp( (sum.g - 0.5f) * 2, 0, 1)) * 2;

				float _fovMask = 1 - sum.r;

				sum.r = _fovMask;
				sum.b = _blurMask;
				


				return sum.rgbb;
			}
			ENDCG
		}
	}
}
