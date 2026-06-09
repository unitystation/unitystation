Shader "Hidden/Noir Effect"
{
	Properties{
		_MainTex("Base (RGB)", RECT) = "white" {}
		_GrainSpeed ("Grain Speed", Range(0.0, 1.0)) = 0.1
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
				uniform float _LensRadius;
				uniform float _GrainSpeed = 0.1f;

				// frag shader
				float4 frag(v2f_img i) : COLOR
				{
					float4 col = tex2D(_MainTex, i.uv);

					//col = max(0.1f, col);
					col *= 1.1f;

					col.r = abs(col.rgb);
					col.gb = col.r;

					//add lens circle effect
					float dist = distance(i.uv, float2(0.5, 0.5))/1.1f;
					col.rgb *= smoothstep(_LensRadius,  _LensRadius - 0.2f, dist);

					
					// Generate random noise
					float2 uv_time = i.uv + frac(_Time.y * _GrainSpeed);
					
					//Lots of magic numbers, the first two are two random primes used as the seed for the grain noise.
					//The hold no value other than being appropriate values I found online
					//The number 43k, is again just an arbritarily large number used so that pixels next to each other are significantly different
					//This is then scaled and shifted from the [0 1] range to the [-1 1] range.
					float noise = (frac(sin(dot(uv_time, float2(12.9898, 78.233))) * 43758.5453) - 0.5) * 2.0;

					// Add noise to the original color
					col.rgb += (noise * 0.06f);
					
					// return col pixel
					return col;
				}

				ENDCG

			}
		}
	Fallback off
}
