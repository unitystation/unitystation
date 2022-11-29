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
				fixed4 mixedLight = lightSample;
				fixed4 screen = tex2D(_MainTex, i.uv);

				mixedLight = mixedLight *1.5;

				float length = sqrt( (mixedLight.r*2) + (mixedLight.g*2) + (mixedLight.b*2));
				//make it 1 Magnitude because brightness is determined by alpha
				fixed3 normaliseColour = (mixedLight / (length/2.25)) ; //* 5.75;
			
				
				fixed3 BalanceLight = clamp(normaliseColour * clamp( occLightSample.a +  mixedLight.a + 0.55, 0,1), 0, 1);
				
				BalanceLight = BalanceLight + (( occLightSample * 0.75 ) * (_obstacleMask));
				//generate bloom 
				fixed3 balancedMixLight =  clamp(normaliseColour*(mixedLight.a - 0.66), 0, 10)*1;
				
				fixed4 NewBalanceLight = fixed4(BalanceLight,0);
				// Blend light with scene.
				fixed4 screenLit =  fixed4( ((screen.rgb*NewBalanceLight+balancedMixLight)) , screen.a);
				
				// Mix Background.
				fixed4 background = tex2D(_BackgroundTex, i.uv);
				float backgroundMask = clamp(occlusionSample.g-(screen.a * 2), 0, 1);
				fixed4 screenLitBackground = background * backgroundMask + screenLit;

				//return (lightSample.a,lightSample.a,lightSample.a,lightSample.a);
				return screenLitBackground;
				//return fixed4(normaliseColour, 1);
			}
			
			ENDCG
		}
	}
}