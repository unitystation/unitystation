// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PostProcess/Compress"{
	Properties{
		_MainTex ("Texture", 2D) = "white" {}
		_Pixelate("Pixelate",Int) = 5
	}
	SubShader{
		Cull Off ZWrite Off ZTest Always
		Pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			int _Pixelate;

			struct appdata{
				float4 vertex:POSITION;
				float2 uv:TEXCOORD0;
			};

			struct v2f{
				float2 uv:TEXCOORD0;
				float4 vertex:SV_POSITION;
			};

			v2f vert(appdata v){
				v2f o;

				float4 vPos = ComputeScreenPos(o.vertex);
				o.uv = (vPos.xy/vPos.w - 0.5)*_ExtendedToSmallTextureScale + 0.5;
				return o;
			}

			fixed4 frag(v2f i):SV_Target
			{
				int2 Pixelate=int2(_Pixelate, _Pixelate);
				fixed4 rescol=float4(0,0,0,0);

				float2 PixelSize=1/float2(_ScreenParams.x,_ScreenParams.y);
				float2 BlockSize=PixelSize*Pixelate;
				float2 CurrentBlock=float2(
					(floor(i.uv.x/BlockSize.x)*BlockSize.x),
					(floor(i.uv.y/BlockSize.y)*BlockSize.y)
				);

				float4 main = tex2D(_MainTex, i.uv.xy);

				rescol=tex2D(_MainTex,CurrentBlock+BlockSize/2);
				rescol+=tex2D(_MainTex,CurrentBlock+float2(BlockSize.x/4,BlockSize.y/4));
				rescol+=tex2D(_MainTex,CurrentBlock+float2(BlockSize.x/2,BlockSize.y/4));
				rescol+=tex2D(_MainTex,CurrentBlock+float2((BlockSize.x/4)*3,BlockSize.y/4));
				rescol+=tex2D(_MainTex,CurrentBlock+float2(BlockSize.x/4,BlockSize.y/2));
				rescol+=tex2D(_MainTex,CurrentBlock+float2((BlockSize.x/4)*3,BlockSize.y/2));
				rescol+=tex2D(_MainTex,CurrentBlock+float2(BlockSize.x/4,(BlockSize.y/4)*3));
				rescol+=tex2D(_MainTex,CurrentBlock+float2(BlockSize.x/2,(BlockSize.y/4)*3));
				rescol+=tex2D(_MainTex,CurrentBlock+float2((BlockSize.x/4)*3,(BlockSize.y/4)*3));
				rescol/=9;

				return float4(main.r, rescol.g, main.b, main.a);
			}

			ENDCG
		}
	}
}
