Shader "PostProcess/Fov Generator"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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
				float4 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 uvObstacle : TEXCOORD1;
				float4 vertex : SV_POSITION;

			};

			sampler2D _MainTex;
			uniform float2 _ExtendedToSmallTextureScale;

			v2f vert (appdata v)
			{
				v2f o;

				//float3 up = mul((float3x3)unity_CameraToWorld, float3(0,-0.01f,0));
				//v.vertex += mul(float4(0,0.05f,0,0), unity_ObjectToWorld);
				o.vertex = UnityObjectToClipPos(v.vertex);

				o.uv = v.uv;

				o.uvObstacle.xy = v.uv ;//+ float2(0.0f, 0.03f);

				float2 scale = float2(0.0f, 0.0f);
				o.uvObstacle.zw = (v.uv.xy * scale) + ((float2(1, 1) - scale) * 0.5f);

				return o;
			}
			
			
			inline float cubicOut(float iTime)
			{
				return (float)(1.0 - (float)--iTime * (float)iTime * (float)iTime * (float)iTime);
				//return (float)(1.0 + (float)--iTime * (float)iTime * (float)iTime);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed fov = 1;


				float2 uv = i.uv;
				float2 uvFrom = i.uvObstacle.xy;
				float2 uvTo = i.uvObstacle.zw;

				for(int i = 0; i < 120; i++)
				{
					half time = i / 120.0f;
					half4 obstacle = tex2D(_MainTex, lerp(uvFrom, uvTo, time));
					fov *= 1-obstacle.r; //saturate(1 - (1 - obstacle)*obstacle.a*m); // was a

				}

				half4 mask = tex2D(_MainTex, uv);
				mask.g = fov;

				return mask.rgbb; 
			}
			ENDCG
		}
	}
}
