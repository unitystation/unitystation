// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

/*
Second / final shader used in Fov Generation. Takes the FloorFovGenerator texture and extends it
to occlude walls. Final output is a texture where visible walls are red, black is occluded floor / walls, and green
is visible floor.
*/
Shader "PostProcess/Fov Generator"
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
                float3 worldpos : TEXCOORD1;
                float4 screenpos : TEXCOORD2;
			};

			sampler2D _MainTex;
			float4 _PositionOffset;

            //how detailed to make wall occlusion checks. Too low = wall occlusion looks messier and jaggier. Too high = performance impact.
            static const int LERP_ITERATIONS = 25;
            // Get texture texel size. Generally We could just read the Screen, but since mask is rendered with its own size we need to get texel from texture.
            uniform float4 _MainTex_TexelSize; 
            

			v2f vert (appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenpos = ComputeScreenPos(o.vertex);
                o.worldpos = mul(unity_ObjectToWorld, v.vertex);
				o.uv = v.uv;

				return o;
			}

            /*
            Lerp from position xy to position target and samples in _MainTex to check if the wall tile is touching green (visible floor), returning 1.0 indicating that this tile should
            be fully visible, 0.0 if invisible
            */
            float checkVisibility(float2 xy, float2 target)
            {
                //sampling logic:
                //it should be fully visible only if red touches green.
                float lastred = 0;
                float visibility = 0.0;
                for (int i = 0; i < LERP_ITERATIONS; i++)
                {
                    half time = i / float(LERP_ITERATIONS);
                    half4 mask = tex2D(_MainTex, lerp(xy, target, time));
                    visibility += lastred * mask.g;

                    lastred = mask.r;                    
                }

                return visibility;
            }

			fixed4 frag (v2f i) : SV_Target
			{
				float2 xy = i.uv;
                half4 mask = tex2D(_MainTex, xy);

                if (mask.g > 0)
                {
                    return mask;
                }                

                //look up to one tile width away from the current pixel in each direction and check if green is touching red.
                float2 TILE_WIDTH = float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * 14.5f; // use pixel size as "step" and scale as required.

                float up = checkVisibility(xy, xy + float2(0, TILE_WIDTH.y));
                float down = checkVisibility(xy, xy + float2(0, -TILE_WIDTH.y));
                float left = checkVisibility(xy, xy + float2(-TILE_WIDTH.x, 0));
                float right = checkVisibility(xy, xy + float2(TILE_WIDTH.x, 0));

                //will be 1.0 if any of the up / down / left / right samples were green (unmasked floor)
                float visibility = up + down + left + right;
                half4 masked = mask * visibility;

                return masked.rgbb;
			}
			ENDCG
		}
	}
}
