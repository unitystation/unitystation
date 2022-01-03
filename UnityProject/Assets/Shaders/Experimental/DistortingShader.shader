// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/DistortingGrabPass" {
	Properties{
		_Intensity("Intensity", Range(0, 50)) = 0
	}
		SubShader{
		Tags { "Queue" = "Transparent" }
			GrabPass { "_BackgroundTexture" }

			Pass {

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				struct v2f {
					float4 pos : SV_POSITION;
					float4 grabPos : TEXCOORD0;
				};


				half _Intensity;

				v2f vert(appdata_base v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.grabPos = ComputeGrabScreenPos(o.pos);
					return o;
				}
				sampler2D _BackgroundTexture;
				half4 frag(v2f i) : SV_Target{
					i.grabPos.x += sin((_Time.y + i.grabPos.y) * _Intensity) / 20;
					half4 color = tex2Dproj(_BackgroundTexture, UNITY_PROJ_COORD(i.grabPos));
					return color;
				}
				ENDCG
			}
	}
		FallBack "Diffuse"
}