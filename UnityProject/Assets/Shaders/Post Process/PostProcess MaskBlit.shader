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
				half4 lightSample = tex2D(_LightMask, i.lightUv);
				fixed4 occLightSample = tex2D(_ObstacleLightMask, i.lightUv);

				float _obstacleMask = occlusionSample.r;
				half4 mixedLight = lightSample;
				fixed4 screen = tex2D(_MainTex, i.uv);

				//Times the light so it's a little bit brighter, this is from the reduced range we have 0 to 0.66 = normal light 0.66 to 1 blown out light
				mixedLight = mixedLight *1.5;

				//We square root and get the "normal" vector of it So the magnitude of the light doesn't play any role in the brightness
				//since brightness is determined by the alpha
				float length = sqrt( (mixedLight.r*2) + (mixedLight.g*2) + (mixedLight.b*2));

				//2.25 Is balancing numbers
				half3 normaliseColour = (mixedLight / (length/2.25)) ; 

				//generate bloom 
				half3 balancedMixLight =  clamp(normaliseColour*(mixedLight.a - 0.66), 0, 10)*1;
				mixedLight.a = (mixedLight.a - 0.5) * 1.1;
				
				//Adding the occlusion and wall stuff
				half3 BalanceLight = clamp(normaliseColour * clamp( occLightSample.a +  mixedLight.a + 0.55, 0,1), 0, 1);

			
				
				//Adding the occlusion and wall stuff
				BalanceLight = BalanceLight + (( occLightSample * 0.75 ) * (_obstacleMask));
				
	
				
				// Blend light with scene.
				half4 screenLit =  fixed4( ((screen.rgb*BalanceLight+balancedMixLight)) , screen.a);
				
				// Mix Background.
				half4 background = tex2D(_BackgroundTex, i.uv);
				float backgroundMask = clamp(occlusionSample.g-(screen.a * 2), 0, 1);
				half4 screenLitBackground = background * backgroundMask + screenLit;

				return screenLitBackground;
			}
			
			ENDCG
		}
	}
}