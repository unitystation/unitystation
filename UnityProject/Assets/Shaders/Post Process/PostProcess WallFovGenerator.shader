Shader "PostProcess/Wall Fov Generator"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_PositionOffset ("Position", Vector) = (0,0,0,0)
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
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _PositionOffset;
            //width / height of a tile square in _MainTex coordinates
            static const float TILE_WIDTH = 0.05;
            //how detailed to make wall occlusion checks. Too low = wall occlusion looks messier. Too high = performance impact.
            static const int LERP_ITERATIONS = 5;
            

			v2f vert (appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;

				return o;
			}

            /*
            Lerp from xy to offset and sample in _MainTex, returning 1.0 indicating that this tile should
            be fully visible, 0.0 if invisible
            */
            float checkVisibility(float2 xy, float2 offset)
            {
                //sampling logic:
                //it should be fully visible only if red touches green.
                float lastred = 0;
                float visibility = 0.0;
                for (int i = 0; i < LERP_ITERATIONS; i++)
                {
                    half time = i / float(LERP_ITERATIONS);
                    half4 mask = tex2D(_MainTex, lerp(xy, xy + offset, time));
                    visibility += lastred * mask.g;

                    lastred = mask.r;                    
                }

                return visibility;
            }

			fixed4 frag (v2f i) : SV_Target
			{
				float2 xy = i.uv;
                float2 center = float2(0.5, 0.5) + _PositionOffset;
                half4 mask = tex2D(_MainTex, xy);

                if (mask.g == 1)
                {
                    return mask;
                }

                //check in four directions for unmasked floor.
                //Wall is only shown if it is touching an unmasked floor                
                float up = checkVisibility(xy, float2(0, TILE_WIDTH));
                float down = checkVisibility(xy, float2(0, -TILE_WIDTH));
                float left = checkVisibility(xy, float2(-TILE_WIDTH, 0));
                float right = checkVisibility(xy, float2(TILE_WIDTH, 0));

                //will be 1.0 if any of the up / down / left / right samples were green (unmasked floor)
                float visibility = up + down + left + right;
                half4 masked = mask * visibility;

                return masked.rgbb;
			}
			ENDCG
		}
	}
}
