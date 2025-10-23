Shader "Hidden/Night Vision"
{
	Properties{
		_MainTex("Base (RGB)", RECT) = "white" {}
		_ScreenTexture("Texture", 2D) = "white" {}
		_Color ("Some Color", Color) = (1,1,1,1) 
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
					uniform sampler2D _ScreenTexture;
					uniform fixed4 _Color;
				uniform float _LensRadius;

				// frag shader
				float4 frag(v2f_img i) : COLOR
				{
					float4 col = tex2D(_MainTex, i.uv);

					col = max(0.1f, col);
					col *= 2.5f;

					col.r = abs(col.rgb);
					col.gb = col.r;

					//add lens circle effect
					//(could be optimised by using texture)
					float dist = distance(i.uv, float2(0.5, 0.5));
					col.rgb *= smoothstep(_LensRadius,  _LensRadius - 0.2f, dist);

					
					col.rgb *= _Color * tex2D(_ScreenTexture, float2(i.uv.x*0.2f, i.uv.y*0.2f + _Time.y/100 % 0.25f));
					// return col pixel
					return col;
				}

				ENDCG

			}
		}
	Fallback off
}
