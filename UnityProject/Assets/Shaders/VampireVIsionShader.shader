Shader "Hidden/Vampire Vision"
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
				uniform float _LensRadius;
					
				static const float3 BT709 = float3(0.2126f, 0.7152f, 0.0722f);

				static const float _RedThreshold = 0.3f; //How close a hue needs to be to red to be considered red
				static const float _RedSoftness = 0.05f;  //Smooths out the cutoff for the above threshold
				static const float _RedDominance = 0.35f; //Red channels below this amplitude are considered less red (overwise dim objects are counted as overly red)
				static const float _HueClamp = 1.0f;       // hue clamp strength        (e.g. 0.65)
				static const float _HueRange = 0.2f;       // hue clamp redness range   (e.g. 0.25)
				static const float _GrainSpeed = 0.1f;

				// frag shader
				float4 frag(v2f_img i) : COLOR
				{
					float4 col = tex2D(_MainTex, i.uv);

					float redness = col.r - max(col.g, col.b);
					float saturation = col.r - min(col.g, col.b);
					
					float rMask = smoothstep(_RedThreshold - _RedSoftness, _RedSoftness + _RedThreshold, redness);
					//Creates a mask texture where white is red pixels, black is non read
					rMask *= smoothstep(0.0f, _RedDominance, col.r);
					rMask *= smoothstep(0.1f, 0.4f, saturation);
					
					float nearRed  = smoothstep(0.0, _HueRange, redness);
                   
					//smoothstep(0.0, uDominance * 0.5, r);
					float3  clampedHue  = float3(col.r,
                        col.g * (1.0 - _HueClamp * nearRed),
                        col.b * (1.0 - _HueClamp * nearRed));
				
    // --- Luma + final mix ---
					//BT709 is a standard colour vector meant to replicate how humans percieve luminosity (greens contributing more etc).
					//This calculates luminosity by seeing how close our colour aligns with that vector
					float luminosity = dot(clampedHue, BT709);
					
					float3 outputColour = lerp(luminosity, clampedHue, rMask);
					float dist = distance(i.uv, float2(0.5, 0.5))/1.1f;
					outputColour.rgb *= smoothstep(_LensRadius,  _LensRadius - 0.2f, dist);
					
					// Generate random noise
					float2 uv_time = i.uv + frac(_Time.y * _GrainSpeed);
					
					//Lots of magic numbers, the first two are two random primes used as the seed for the grain noise.
					//The hold no value other than being appropriate values I found online
					//The number 43k, is again just an arbritarily large number used so that pixels next to each other are significantly different
					//This is then scaled and shifted from the [0 1] range to the [-1 1] range.
					float noise = (frac(sin(dot(uv_time, float2(12.9898, 78.233))) * 43758.5453) - 0.5) * 2.0;

					// Add noise to the original color
					outputColour.rgb += (noise * 0.06f);
					
					
					// return col pixel
					return float4(outputColour, 1.0f);
				}

				ENDCG

			}
		}
	Fallback off
}
