Shader "Hidden/Night Vision"
{
	Properties{
		_MainTex("Base (RGB)", RECT) = "white" {}
	}

		SubShader{
			Pass {
				ZTest Always Cull Off ZWrite Off
				Fog { Mode off }

				CGPROGRAM
					#pragma vertex vert_img
					#pragma fragment frag
					#pragma fragmentoption ARB_precision_hint_fastest
					#include "UnityCG.cginc"

				// frag shaders data
				uniform sampler2D _MainTex;
				uniform float4 _Luminance;
				uniform float _LensRadius;

				// frag shader
				float4 frag(v2f_img i) : COLOR
				{

					float4 col = tex2D(_MainTex, i.uv);

					//obtain luminance value
					col = dot(col, _Luminance);

					//add lens circle effect
					//(could be optimised by using texture)
					float dist = distance(i.uv, float2(0.5, 0.5));
					col *= smoothstep(_LensRadius,  _LensRadius - 0.34, dist);

					//add rb to the brightest pixels
					col.r = max(col.r - 0.75, 0) * 4;

					// return col pixel
					return col;
				}

				ENDCG

			}
		}
	Fallback off
}
