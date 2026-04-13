Shader "Hidden/Crit State"
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
				uniform int _CurrentHealth = 0;
					
				static const float _GrainSpeed = 0.1f;
				static const int vignetteStart = 25;
				static const int vignetteEnd = -100;
				static const int greyScaleStart = 25;
				static const int greScaleEnd = -15;
				static const int redTintStart = 10;
				static const int redTintEnd = -60;
				static const int darkeningStart = 0;
				static const int darkeningEnd = -100;
					
				static const float3 BT709 = float3(0.2126f, 0.7152f, 0.0722f);
				static const float noiseIntensity = 0.04f;
					
				///As these effects trigger instantly, please lerp the _CurrentHealth input to this shader
				///else you might have a jarring player experience.
				float4 frag(v2f_img i) : COLOR
				{
					float4 col = tex2D(_MainTex, i.uv);
					float luminosity = dot(col.rgb, BT709);
				
					float greyScaleIntensity = smoothstep(greyScaleStart, greScaleEnd, _CurrentHealth);
					float redTintIntensity = smoothstep(redTintStart, redTintEnd, _CurrentHealth) * 0.75f;
					float darkeningIntensity = smoothstep(darkeningStart, darkeningEnd, _CurrentHealth);
					float vignetteIntensity = smoothstep(vignetteStart, vignetteEnd, _CurrentHealth);
					
					float lensRadius = lerp(1.2f, 0.4f, min(vignetteIntensity, 0.7f));
					float softness = (1 - step(vignetteStart, _CurrentHealth)) * 0.4f;

					//Vignette
					float2 centeredUV = i.uv - float2(0.5, 0.5);
					centeredUV.x *= _ScreenParams.x / _ScreenParams.y; // correct for aspect ratio
					float dist = length(centeredUV);
					float vignette = max(0.1f,smoothstep(lensRadius,  lensRadius - softness, dist));
					
					// Generate random noise
					float2 uv_time = i.uv + frac(_Time.y * _GrainSpeed);
					float noise = (frac(sin(dot(uv_time, float2(12.9898, 78.233))) * 43758.5453) - 0.5) * 2.0 * noiseIntensity * greyScaleIntensity;

					//Colour effects
					float4 redFilter = float4(luminosity, luminosity * (1 - redTintIntensity), luminosity * (1 - redTintIntensity), 1);
					float4 outputColour = lerp(col, float4(luminosity,luminosity,luminosity,1), greyScaleIntensity); //Desaturate by stage one intensity
					outputColour = lerp(outputColour, redFilter, redTintIntensity); //Make red by stage two intensity
					outputColour = lerp(outputColour, float4(0.0f,0.0f,0.0f,1.0f), min(darkeningIntensity, 0.6f));

					//Apply noise
					outputColour += noise; 

					//Apply vignette
					outputColour *= vignette;

					
					return outputColour;
				}

				ENDCG

			}
		}
	Fallback off
}
