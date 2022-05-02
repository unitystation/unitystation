
Shader "Custom/WarpShaderPassGrab" {
	Properties{
		//_MainTex("MainTex",2D) = "white"{}
		_EffectRadius("EffectRadius", Range(0, 1)) = 0.1
		_EffectAngle("EffectAngle", Range(0 , 4.)) = 2.0

		_xPos("xPos", Range(0, 1)) = 0.5
		_yPos("yPos", Range(0 , 1)) = .5
	}
	SubShader
	{
		Tags 
		{
			
			"Queue" = "Transparent" 
		}
		GrabPass { "_BackgroundTexture" }
		Pass {
			ZWrite Off
			//Blend SrcAlpha OneMinusSrcAlpha
		
			//Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"


			float2 vec2(float x,float y) { return float2(x,y); }


			float vec(float x) { return float(x); }


			struct v2f {
				float4 pos : SV_POSITION;
				//float2 uv : TEXCOORD0; //grabPos
				float4 grabPos : TEXCOORD0;
				//float4 uvgrab : TEXCOORD1;
				float4 projPos : TEXCOORD2;
			};

			sampler2D _BackgroundTexture;

			uniform float _EffectRadius;
			uniform float _EffectAngle;

			uniform float _xPos;
			uniform float _yPos;

			uniform half4 _MainTex_ST;

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				//o.uv = v.uv;
				//o.uvgrab = ComputeGrabScreenPos(v.vertex);
				o.grabPos = ComputeGrabScreenPos(o.pos);

				//o.position_uv = _MainTex_ST.xy * v.texCoord + _MainTex_ST.zw;

				//o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;

				return o;
			}

			#define PI 3.14159

			half4 frag(v2f i) : SV_Target
			{
				float effectRadius = _EffectRadius;
				float effectAngle = _EffectAngle * PI;
				float2 center = vec2(_yPos, _xPos);
				center = center == vec2(0., 0.) ? vec2(.5, .5) : center;

				float2 uv = i.grabPos.xy / 1 - center;

				float len = length(uv * vec2(_ScreenParams.xy.x / _ScreenParams.xy.y,1.0));
				float angle = atan2(uv.y,uv.x) + effectAngle * smoothstep(effectRadius, 0., len);
				float radius = length(uv);

				i.grabPos.x += radius * cos(angle) + center;//i.grabpos
				i.grabPos.y += radius * sin(angle) + center;
				half4 color = tex2Dproj(_BackgroundTexture, UNITY_PROJ_COORD(i.grabPos));
				//half4 splash = tex2D(_BackgroundTexture, UNITY_PROJ_COORD(i.grabPos));
				return color ;
			}
			ENDCG
		}
	}
}
