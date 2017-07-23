/*

That shader is used after all light obstacles have been drawn to draw one pixel wide white corner over it.

*/


Shader "Light2D/Obstacle Texture Post Porcessor" {
SubShader {
	Tags {"Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 100
	
	ZWrite Off
	Blend Off
	ZTest Always
	Cull Off

	Pass {  
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
			};

			uniform sampler2D _MainTex;
			
			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = v.vertex;
				o.color = v.color;
				return o;
			}

			half4 frag (v2f i) : COLOR
			{
				clip(i.color.a - 0.01);

				return i.color;
			}
		ENDCG
	}
}

}
