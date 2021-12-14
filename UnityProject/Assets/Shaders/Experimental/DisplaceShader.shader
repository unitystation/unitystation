Shader "Unlit/DistortionParticle"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Magnitude("Magnitude", Float) = 1
	}
		SubShader
		{
			Tags
			{
				"RenderType" = "Transparent"
				"Queue" = "Transparent"
			}
			Pass
			{
				Blend One One
				Cull Off
				ZWrite Off
				ZTest Always

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag			
				#include "UnityCG.cginc"

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
					float4 color : COLOR;
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
					float alpha : TEXCOORD1;
					float4 projPos : TEXCOORD2;
				};

				sampler2D _MainTex;
				sampler2D_float _CameraDepthTexture;
				float4 _MainTex_ST;
				float _Magnitude;

				v2f vert(appdata v)
				{
					v2f o;

					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					o.alpha = v.color.a;
					o.projPos = ComputeScreenPos(o.vertex);
					COMPUTE_EYEDEPTH(o.projPos.z);

					return o;
				}

				float2 frag(v2f i) : SV_Target
				{
					float sceneEyeDepth = DECODE_EYEDEPTH(tex2D(_CameraDepthTexture, i.projPos.xy / i.projPos.w));
					float zCull = sceneEyeDepth > i.projPos.z;
					float3 data = UnpackNormal(tex2D(_MainTex, i.uv)).xyz;
					float scale = data.b * i.alpha * _Magnitude;
					return data.rg * scale;// *zCull;
				}
				ENDCG
			}
		}
}