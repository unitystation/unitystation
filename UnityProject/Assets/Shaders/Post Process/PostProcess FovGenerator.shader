Shader "PostProcess/Fov Generator"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_PositionOffset ("Position", Vector) = (0,0,0,0)
		_OcclusionSpread ("Occlusion Spread", float) = 0
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
			float4 _PositionOffset;
			float _OcclusionSpread;
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

			fixed4 frag (v2f i) : SV_Target
			{
				fixed fov = 1;

				float2 uv = i.uv;
				float2 uvFrom = i.uvObstacle.xy;
				float2 uvTo = i.uvObstacle.zw + _PositionOffset;
				float spread = 1 - _OcclusionSpread;

				for(int i = 0; i < 200; i++)
				{
					half time = i / 200.0f;
					half4 obstacle = tex2D(_MainTex, lerp(uvFrom, uvTo, time));
					fov *= 1 - (obstacle.r * spread);
				}

				half4 mask = tex2D(_MainTex, uv);
				mask.g = fov;

				return mask.rgbb; 
			}
			ENDCG
		}
	}
}
