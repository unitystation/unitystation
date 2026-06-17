Shader "UI/MenuOverlay"
{
	Properties
	{
		_VignetteColor ("Vignette Color", Color) = (0,0,0,1)
		_VignetteIntensity ("Vignette Intensity", Range(0,1)) = 0
		_VignetteSoftness ("Vignette Softness", Range(0,0.7)) = 0.45
		_GrainIntensity ("Grain Intensity", Range(0,1)) = 0
		_GrainScale ("Grain Scale", Float) = 900
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
		Cull Off ZWrite Off ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

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
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			float4 _VignetteColor;
			float _VignetteIntensity;
			float _VignetteSoftness;
			float _GrainIntensity;
			float _GrainScale;

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float hash (float2 p)
			{
				return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float dist = length(i.uv - 0.5);
				float vignette = smoothstep(0.75 - _VignetteSoftness, 0.75, dist) * _VignetteIntensity;

				float2 cell = floor(i.uv * _GrainScale);
				float frame = floor(_Time.y * 24.0);
				float noise = hash(cell + float2(frame, frame * 1.37));
				float grain = abs(noise - 0.5) * 2.0 * _GrainIntensity;
				float3 grainColour = noise > 0.5 ? float3(1, 1, 1) : float3(0, 0, 0);

				float alpha = saturate(vignette + grain);
				float3 colour = lerp(grainColour, _VignetteColor.rgb, vignette / (vignette + grain + 1e-4));
				return fixed4(colour, alpha);
			}
			ENDCG
		}
	}
}
