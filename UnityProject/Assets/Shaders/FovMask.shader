Shader "Custom/Fov Mask" {

	// Special material used to render Fov mask.
	// It has two modes that are controlled by _FovBlurSwitch.
	// First when _FovBlurSwitch is used to render scene with replacement shader. It will render objects in to R Channel.
	// Second one when _FovBlurSwitch is used for FOV mask renderer to render it in to G Channel.
	Properties
    {
		_FovBlurSwitch ("FovBlurSwitch", Float) = 0
    }

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

			float _FovBlurSwitch;

			v2f vert(appdata_t v)
			{ 
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				return float4(1 - _FovBlurSwitch, _FovBlurSwitch, 0, 0); 
			}
			ENDCG
		}
	}


	FallBack "Diffuse"
}
