Shader "Custom/DrunkShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "black" {}
		_LightAmount("Light Amount", Range(0, 10)) = 5.0
		_CosAmount("Vertical Pan Amount", Range(0 , 0.1)) = 0.05
		_SinAmount("Horizontal Pan Amount", Range(0 , 0.1)) = 0.05
		_Waves("Waves", Range(0 , 0.5)) = 0.25
		_Speed("Speed", Range(0 , 1)) = 0.5
		_DoubleVision("Double Vision", Range(0 , 0.02)) = 0.01
	}
		Subshader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vertex_shader
			#pragma fragment pixel_shader
			#pragma target 2.0
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			uniform float _LightAmount;
			uniform float _CosAmount;
			uniform float _SinAmount;
			uniform float _Waves;
			uniform float _Speed;
			uniform float _DoubleVision;

			float4 vertex_shader(float4 vertex:POSITION) :SV_POSITION
			{
				return UnityObjectToClipPos(vertex);
			}

			float4 pixel_shader(float4 vertex:SV_POSITION) : COLOR
			{
				vector <float, 2> uv = vertex.xy / _ScreenParams.xy;
			
				// Flip sampling of the Texture if DirectX
				#if UNITY_UV_STARTS_AT_TOP
						uv.y = 1 - uv.y;
				#endif

				uv.x += cos(uv.y * _Waves + _Time.g) * _CosAmount;
				uv.y += sin(uv.x * _Waves + _Time.g) * _SinAmount;

				float offset = sin(_Time.g * _Speed) * _DoubleVision;
				float4 a = tex2D(_MainTex,uv);
				float4 b = tex2D(_MainTex,uv - float2(sin(offset),0.0));
				float4 c = tex2D(_MainTex,uv + float2(sin(offset),0.0));
				float4 d = tex2D(_MainTex,uv - float2(0.0,sin(offset)));
				float4 e = tex2D(_MainTex,uv + float2(0.0,sin(offset)));
				return (a + b + c + d + e) / _LightAmount;
			}
			ENDCG
		}
	}
}
