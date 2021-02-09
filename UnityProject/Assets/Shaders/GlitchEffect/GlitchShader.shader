// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// This work is licensed under a Creative Commons Attribution 3.0 Unported License.
// http://creativecommons.org/licenses/by/3.0/deed.en_GB
//
// You are free:
//
// to copy, distribute, display, and perform the work
// to make derivative works
// to make commercial use of the work


Shader "Hidden/GlitchShader" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_DispTex ("Base (RGB)", 2D) = "bump" {}
	_Intensity ("Glitch Intensity", Range(0.1, 1.0)) = 1
	_ColorIntensity("Color Bleed Intensity", Range(0.1, 1.0)) = 0.2
}

SubShader {
	Pass {
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma fragmentoption ARB_precision_hint_fastest 
		
		#include "UnityCG.cginc"
		
		uniform sampler2D _MainTex;
		uniform sampler2D _DispTex;
		float _Intensity;
		float _ColorIntensity;

		fixed4 direction;

		float filterRadius;
		float flip_up, flip_down;
		float displace;
		float scale;
		
		struct v2f {
			float4 pos : POSITION;
			float2 uv : TEXCOORD0;
		};
		
		v2f vert( appdata_img v )
		{
			v2f o;
			o.pos = UnityObjectToClipPos (v.vertex);
			o.uv = v.texcoord.xy;
			
			return o;
		}
		
		half4 frag (v2f i) : COLOR
		{
			half4 normal = tex2D (_DispTex, i.uv.xy * scale);
			
			i.uv.y -= (1 - (i.uv.y + flip_up)) * step(i.uv.y, flip_up) + (1 - (i.uv.y - flip_down)) * step(flip_down, i.uv.y);

			i.uv.xy += (normal.xy - 0.5) * displace * _Intensity;
			
			half4 color = tex2D(_MainTex,  i.uv.xy);
			half4 redcolor = tex2D(_MainTex, i.uv.xy + direction.xy * 0.01 * filterRadius * _ColorIntensity);
			half4 greencolor = tex2D(_MainTex,  i.uv.xy - direction.xy * 0.01 * filterRadius * _ColorIntensity);

			color += fixed4(redcolor.r, redcolor.b, redcolor.g, 1) *  step(filterRadius, -0.001);
			color *= 1 - 0.5 * step(filterRadius, -0.001);

			color += fixed4(greencolor.g, greencolor.b, greencolor.r, 1) *  step(0.001, filterRadius);
			color *= 1 - 0.5 * step(0.001, filterRadius);
			
			return color;
		}
		ENDCG
	}
}

Fallback off

}