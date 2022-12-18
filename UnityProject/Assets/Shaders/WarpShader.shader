

Shader "Custom/WarpShader"
{
	Properties
	{
		_EffectRadius("EffectRadius", Range(0, 2)) = 0.11
		_EffectAngle("EffectAngle", Range(0 , 32.)) = 5.5
	}
	SubShader
		{
		Tags
		{
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"Queue" = "Transparent"
		}
		GrabPass { }
		Pass
		{
			Name "UNITY_PASS_FORWARDBASE"
            Tags {
                "LightMode"="ForwardBase"
            }
			ZWrite Off
			ZTest Off
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#define UNITY_PASS_FORWARDBASE
			#include "UnityCG.cginc"
			#pragma multi_compile_fwdbase

			float2 vec2(float x,float y) { return float2(x,y); }
			float2 vec2(float x) { return float2(x,x); }

			float vec(float x) { return float(x); }

			sampler2D _GrabTexture;

			struct VertexInput
			{
				float4 vertex : POSITION;
				float2 uv:TEXCOORD0;
				float4 tangent : TANGENT;
				float3 normal : NORMAL;
			};
			struct VertexOutput
			{
				float4 pos : SV_POSITION;
				float2 uv:TEXCOORD0;
				float4 projPos : TEXCOORD2;
			};
			uniform float _EffectRadius;
			uniform float _EffectAngle;

			VertexOutput vert(VertexInput v)
			{
				VertexOutput o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeGrabScreenPos(o.pos);
				o.uv = v.uv;

				return o;
			}

			#define PI 3.14159

			fixed4 frag(VertexOutput i) : SV_Target
			{
				float effectRadius = _EffectRadius;
				float effectAngle = _EffectAngle * PI;
				float2 center = vec2(0.5,0.5);

				center = center == vec2(0., 0.) ? vec2(.5, .5) : center;

				float2 uv = i.uv.xy - center;

				float len = length(uv * vec2(_ScreenParams.xy.x / _ScreenParams.xy.y, _ScreenParams.xy.x / _ScreenParams.xy.y));
				float angle = atan2(uv.y,uv.x) + (effectAngle / unity_OrthoParams.xy) * smoothstep(effectRadius, 0., len) ;
				float radius = length(uv);
				float2 spiral = vec2(radius * cos(angle), radius * sin(angle));
				i.projPos.xy = i.projPos.xy - uv.xy;
				i.projPos.xy += spiral;

				fixed4 output = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(i.projPos) );
		
				return output;
			}
			ENDCG
			}
		}
}
