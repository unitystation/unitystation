Shader "PostProcess/Mask Blit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Mask ("Texture", 2D) = "white" {}
		_LightMask ("Texture", 2D) = "white" {}
		_Ambient ("Ambient", Float) = 1
		_LightMultiplier ("LightMultiplier", Float) = 1
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

			};

			v2f vert (appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv =  v.uv;

				return o;
			}
			
			sampler2D _MainTex;
			sampler2D _Mask;
			sampler2D _LightMask;
			float _Ambient;
			float _LightMultiplier;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 screen = tex2D(_MainTex, i.uv);
				fixed4 maskSample = tex2D(_Mask, i.uv);
				fixed4 lightSample = tex2D(_LightMask, i.uv);

				fixed4 screenUnlit = (screen * _Ambient);

				// light
				// TODO add bloom
				float _additiveLightPow = 8;
				float _additiveLightAdd = 0.1;
				//float4 _lightClamped = clamp(lightSample, 1-_Alpha, 1);

				half3 bloom = (screenUnlit.rgb + _additiveLightAdd) * pow(lightSample.rgb, _additiveLightPow) * step(0.005, _additiveLightPow);
				fixed4 screenLit = screenUnlit + fixed4(screenUnlit.rgb * lightSample.rgb * _LightMultiplier + bloom, screenUnlit.a) * 1;
				//
				
				//* _Alpha;
				//return screenUnlit;

				float mask = 1 - clamp(maskSample.g + maskSample.r, 0, 1);
				return lerp(screenLit, screenUnlit, mask);
			}
			ENDCG
		}
	}
}
