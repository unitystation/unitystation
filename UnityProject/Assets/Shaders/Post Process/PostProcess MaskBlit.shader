Shader "PostProcess/Mask Blit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
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
				float2 occlusionUv : TEXCOORD2;
				float4 vertex : SV_POSITION;
			};
					
			sampler2D _OcclusionMask;
			sampler2D _ObstacleLightMask;
			sampler2D _LightMask;
			sampler2D _MainTex;
			sampler2D _BackgroundTex;

			float4 _LightTransform;
			float4 _OcclusionTransform;

			float4 _AmbLightBloomSA;
			float _BackgroundMultiplier;
			
			v2f vert (appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.lightUv = (v.uv - 0.5 + _LightTransform.xy) * _LightTransform.zw + 0.5;
				o.occlusionUv = (v.uv - 0.5 + _OcclusionTransform.xy) * _OcclusionTransform.zw + 0.5;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// Mix Lights.
				fixed4 occlusionSample = tex2D(_OcclusionMask, i.occlusionUv);
				fixed4 lightSample = tex2D(_LightMask, i.lightUv);
				fixed4 occLightSample = tex2D(_ObstacleLightMask, i.lightUv);

				float _obstacleMask = occlusionSample.r;
				fixed4 mixedLight = lightSample * (1-_obstacleMask) +((occLightSample-0.2)*0.5) * _obstacleMask;
				fixed4 screen = tex2D(_MainTex, i.uv);

				//expand colour range
				fixed4 doubleBalanceLight = mixedLight*2;
				
		
				//generate bloom 
				fixed4 balancedMixLight = (lightSample - 0.75)*0.5;
				balancedMixLight.r = clamp(balancedMixLight.r,0, 10);
				balancedMixLight.g = clamp(balancedMixLight.g,0, 10);
				balancedMixLight.b = clamp(balancedMixLight.b,0, 10);
				
				// Blend light with scene.
				fixed4 screenLit =  fixed4( ((screen.rgb*doubleBalanceLight)+balancedMixLight) , screen.a);
				
				// Mix Background.
				fixed4 background = tex2D(_BackgroundTex, i.uv);
				float backgroundMask = clamp(occlusionSample.g-(screen.a * 2), 0, 1);
				fixed4 screenLitBackground = background * backgroundMask + screenLit;

				return screenLitBackground;
			}
			
			ENDCG
		}
	}
}