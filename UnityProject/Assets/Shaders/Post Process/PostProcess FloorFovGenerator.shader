/*
Generates the occlusion mask on the floor using the obstacle mask (which has walls as red, everything else as black) as the input texture.
Outputs a texture which has red for walls, black for occluded areas, and green for non-occluded floor. Walls are left red.
*/
Shader "PostProcess/Floor Fov Generator"
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
			float3 _Distance;

			v2f vert (appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uvObstacle.xy = v.uv ;

				float2 scale = float2(0.0f, 0.0f);
				o.uvObstacle.zw = (v.uv.xy * scale) + ((float2(1, 1) - scale) * 0.5f);

				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// Project FoV.
				fixed fov = 1;

				float2 uv = i.uv;
				float2 uvFrom = i.uvObstacle.xy;
				float2 uvTo = i.uvObstacle.zw + _PositionOffset;
				float spread = 1 - _OcclusionSpread;

				for(int i = 0; i < 100; i++)
				{
					half time = i / 100.0f;
					half4 obstacle = tex2D(_MainTex, lerp(uvFrom, uvTo, time));
					fov *= 1 - (obstacle.r * spread);
				}

				// Limit FoV by view distance.
				float _horizonSmooth = _Distance.z;
				float _distance = _Distance.x;
				float _uvYScale = _Distance.y;

				// Flatten the uv space for XY to be proportional. 
				// Add position offset.
				float2 uvScale = float2(1, _uvYScale);
				float2 scalepoint = ((uv.xy - _PositionOffset.xy) * uvScale) + ((float2(1, 1) - uvScale) * 0.5f);

				float distanceFromCenter = clamp(1 - distance(float2(0.5f, 0.5f), scalepoint), 0, 1) - _distance - 1;
				float smoothedDistance = clamp(distanceFromCenter * _horizonSmooth, 0, 1);

				half4 mask = tex2D(_MainTex, uv);

				mask.g = fov * smoothedDistance;

				return mask.rgbb; 
			}
			ENDCG
		}
	}
}
