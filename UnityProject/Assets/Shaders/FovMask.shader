Shader "Custom/Fov Mask" {

	// Special material used to render Fov mask.
	// It has two modes that are controlled by _FovBlurSwitch.
	// First when _FovBlurSwitch is used to render scene with replacement shader. It will render objects in to R Channel.
	// Second one when _FovBlurSwitch is used for FOV mask renderer to render it in to G Channel.
	Properties
    {
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
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
				float2 texcoord : TEXCOORD0;
			};

			struct v2f 
			{
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float _FovBlurSwitch;
			float _DemaskSwitch;

			v2f vert(appdata_t v)
			{ 
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				half4 col = tex2D(_MainTex, i.texcoord); 

				return float4((1 - _FovBlurSwitch) * (1 - _DemaskSwitch) * col.a * col.r * 1000, _FovBlurSwitch, 0, 0); 
			}
			ENDCG
		}
	}


	FallBack "Diffuse"
}
