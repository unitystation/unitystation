Shader "Custom/Fov Hard Mask" {


	SubShader {
		Tags{ "Queue" = "Transparent+1000" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 100

		ZWrite Off

		// Note: Additional blending will add colors together.
		Blend One One

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata_t 
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
			};

			struct v2f 
			{
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				return float4(1,0,0,0);
			}
			ENDCG
		}
	}


	FallBack "Diffuse"
}
