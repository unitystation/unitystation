/*

Used to create normal map buffer.

*/

Shader "Light2D/Internal/Normal Mapped Light" {
SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 100
	
	ZWrite Off
	Blend OneMinusDstColor One

	Pass {  
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				//float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 normalTexcoord : TEXCOORD0;
				half2 lightTexcoord : TEXCOORD1;
				half3 lightPos : TEXCOORD2;
			};

			uniform sampler2D _MainTex;
			uniform sampler2D _NormalsBuffer;
			uniform half3 _LightPos;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.normalTexcoord = o.vertex*0.5 + 0.5;
				o.lightTexcoord = o.vertex*0.5 + 0.5;
				o.lightPos = mul(UNITY_MATRIX_VP, half4(_LightPos.xy, 0, 1));
				o.lightPos.z = _LightPos.z;
				o.lightPos.xy = o.lightPos.xy*0.5 + 0.5;

				#if UNITY_UV_STARTS_AT_TOP
				o.normalTexcoord.y = 1 - o.normalTexcoord.y;
				o.lightTexcoord.y = 1 - o.lightTexcoord.y;
				o.lightPos.y = 1 - o.lightPos.y;
				#endif

				return o;
			}

			half4 frag (v2f i) : COLOR
			{
				half4 light = tex2D(_MainTex, i.lightTexcoord);
				half3 normal = normalize(tex2D(_NormalsBuffer, i.normalTexcoord)*2.0 - 1.0);
				half3 revLightDir = normalize(i.lightPos - half3(i.lightTexcoord, 0));
				half lightness = max(0, -dot(normal, revLightDir));
				return half4(light.rgb*lightness, light.a);
			}
		ENDCG
	}
}


}