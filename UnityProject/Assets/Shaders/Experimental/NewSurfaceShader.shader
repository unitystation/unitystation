// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/WaterGrab"
{
	Properties
	{
		_Colour("Colour", Color) = (1,1,1,1)
		_MainTex("Noise text", 2D) = "bump" {}
		_Magnitude("Magnitude", Range(0,1)) = 0.05
	}

		SubShader
		{
			Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Opaque"}
			ZWrite On Lighting Off Cull Off Fog { Mode Off } Blend One Zero

			GrabPass { "_GrabTexture" }

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				sampler2D _GrabTexture;
				fixed4 _Colour;
				sampler2D _MainTex;
				float  _Magnitude;

				struct vin
				{
					float4 vertex : POSITION;
					float4 color : COLOR;
					float2 texcoord : TEXCOORD0;

				};

				struct v2f
				{
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float2 texcoord : TEXCOORD0;
					float4 uvgrab : TEXCOORD1;

				};

				float4 _MainTex_ST;

				// Vertex function 
				v2f vert(vin v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.color = v.color;
					o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);


				#if UNITY_UV_STARTS_AT_TOP
					float scale = -1.0;
				#else
					float scale = 1.0;
				#endif            

					o.uvgrab.xy = (float2(o.vertex.x, (o.vertex.y)* scale) + o.vertex.w) * 0.5;
					o.uvgrab.zw = o.vertex.zw;

					float4 top = UnityObjectToClipPos(float4(0, 0.5, 0, 1));
					top.xy /= top.w;

					o.uvgrab.y = 1 - (o.uvgrab.y + top.y);

					return o;
				}

				// Fragment function
				half4 frag(v2f i) : COLOR
				{

					half4 bump = tex2D(_MainTex, i.texcoord);
					half2 distortion = UnpackNormal(bump).rg;


					i.uvgrab.xy += distortion * _Magnitude;
					fixed4 col = tex2D(_GrabTexture, i.uvgrab);
					return col * _Colour;
				}

				ENDCG
			}
		}
}