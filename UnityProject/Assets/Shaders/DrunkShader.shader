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
					#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile DUMMY PIXELSNAP_ON
			#pragma target 2.0
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			uniform float _LightAmount;
			uniform float _CosAmount;
			uniform float _SinAmount;
			uniform float _Waves;
			uniform float _Speed;
			uniform float _DoubleVision;

		struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };
 
            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                half2 texcoord  : TEXCOORD0;
            };

			v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }
        

			fixed4 frag(v2f IN) : COLOR
            {
				
            	float2 uv = IN.texcoord;
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
