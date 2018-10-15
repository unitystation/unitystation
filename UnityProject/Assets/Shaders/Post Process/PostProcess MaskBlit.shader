Shader "PostProcess/Mask Blit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_BackgroundTex ("Background", 2D) = "black" {}
		_LightTex ("LightTexture", 2D) = "white" {}
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
				float2 lightUv : TEXCOORD1;
				float4 vertex : SV_POSITION;

			};
						
			sampler2D _LightTex;
			sampler2D _MainTex;
			sampler2D _BackgroundTex;
			float4 _AmbLightBloomSA;
			float4 _LightTransform;
			float _BackgroundMultiplier;

			v2f vert (appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.lightUv = (v.uv - 0.5 + _LightTransform.xy) * _LightTransform.zw + 0.5;

				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 screen = tex2D(_MainTex, i.uv);
				fixed4 lightSample = tex2D(_LightTex, i.lightUv);

				float ambient = _AmbLightBloomSA.r;
				float lightMultyplier = _AmbLightBloomSA.g;
				float bloomSensitivity = _AmbLightBloomSA.b;
				float bloomAdd = _AmbLightBloomSA.a;

				fixed4 screenUnlit = (screen * ambient);

				// Mix Light.
				half3 bloom = (screenUnlit.rgb + bloomAdd) * pow(lightSample.rgb, bloomSensitivity) * step(0.005, bloomSensitivity);
				fixed4 screenLit = screenUnlit + fixed4(screenUnlit.rgb * lightSample.rgb * lightMultyplier + bloom, screenUnlit.a) * 1;

				// Mix Background.
				fixed4 background = tex2D(_BackgroundTex, i.uv) * _BackgroundMultiplier;
				float backgroundMask = clamp(1 - (screen.a * 2),0,1);
				fixed4 screenLitBackground = background * backgroundMask + screenLit;

				return screenLitBackground;
			}
			
			ENDCG
		}
	}
}