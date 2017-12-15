// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

/*

Base code for standard light shaders.
Light is computed by path tracking with fixed number of steps (PATH_TRACKING_SAMPLES).

*/


#ifndef LIGHT_BASE_INCLUDED
#define LIGHT_BASE_INCLUDED

#pragma glsl_no_auto_normalization

struct light2d_fixed_data_t {
	float4 vertex : POSITION;
	float2 texcoord : TEXCOORD0;
	float4 color : COLOR0;
	float2 texcoord1 : TEXCOORD1;
};

struct light2d_fixed_v2f {
	float4 vertex : SV_POSITION;
	half2 texcoord : TEXCOORD0;
	half4 color : COLOR0;
	#ifdef PERSPECTIVE_CAMERA
	half2 texcoord1 : TEXCOORD1;
	float4 projVertex : COLOR1;
	float zDistance : TEXCOORD2;
	#else
	half2 thisPos : TEXCOORD2;
	half2 centerPos : TEXCOORD1;
	#endif
	half2 aspect : TEXCOORD3;
};
			
uniform sampler2D _ObstacleTex;
uniform sampler2D _MainTex;
uniform half _ObstacleMul;
uniform half _EmissionColorMul;
uniform float2 _ExtendedToSmallTextureScale;
#ifdef UNITY_HALF_TEXEL_OFFSET
uniform half2 _PosOffset;
#endif

light2d_fixed_v2f light2d_fixed_vert (light2d_fixed_data_t v)
{
	light2d_fixed_v2f o;
	o.vertex = UnityObjectToClipPos(v.vertex);
	o.texcoord = v.texcoord;

	#ifdef PERSPECTIVE_CAMERA
	o.texcoord1 = v.texcoord1;
	o.projVertex = o.vertex;
	o.zDistance = mul(unity_ObjectToWorld, v.vertex).z;
	#else
	float4 vPos = ComputeScreenPos(o.vertex);
	o.thisPos = (vPos.xy/vPos.w - 0.5)*_ExtendedToSmallTextureScale + 0.5;
	#if LIGHT2D_XY_PLANE
	float4 cPos = ComputeScreenPos(mul(UNITY_MATRIX_VP, float4(v.texcoord1, 0, 1)));
	#elif LIGHT2D_XZ_PLANE
	float4 cPos = ComputeScreenPos(mul(UNITY_MATRIX_VP, float4(v.texcoord1.x, 0, v.texcoord1.y, 1)));
	#endif
	o.centerPos = (cPos.xy/cPos.w - 0.5)*_ExtendedToSmallTextureScale + 0.5;
	#ifdef UNITY_HALF_TEXEL_OFFSET
	o.thisPos += _PosOffset;
	o.centerPos += _PosOffset;
	#endif
	#endif

	o.color = v.color;
	o.aspect = half2(_ScreenParams.x/_ScreenParams.y, 1);

	return o;
}

half4 light2_fixed_frag (light2d_fixed_v2f i) : COLOR
{
    half4 tex = tex2D(_MainTex, i.texcoord);

	#ifdef PERSPECTIVE_CAMERA
	half4 vPos = ComputeScreenPos(i.projVertex);
	half2 thisPos = (vPos.xy/vPos.w - 0.5)*_ExtendedToSmallTextureScale + 0.5;
	half4 cPos = ComputeScreenPos(mul(UNITY_MATRIX_VP, float4(i.texcoord1, i.zDistance, 1)));
	half2 centerPos = (cPos.xy/cPos.w - 0.5)*_ExtendedToSmallTextureScale + 0.5;
	#ifdef UNITY_HALF_TEXEL_OFFSET
	thisPos += _PosOffset;
	centerPos += _PosOffset;
	#endif
	#else
	half2 thisPos = i.thisPos;
	half2 centerPos = i.centerPos;
	#endif

	half sub = 1.0/PATH_TRACKING_SAMPLES;
	half len = length((thisPos - centerPos)*i.aspect);
	half m = _ObstacleMul*sub*len;
			
	half4 col = i.color*tex*tex.a*i.color.a;

	half pos = 0;
	
	for(int i = 0; i < PATH_TRACKING_SAMPLES; i++)
	{
		pos += sub; 
		half4 obstacle = tex2D(_ObstacleTex, lerp(centerPos, thisPos, pos));
		col *= saturate(1 - (1 - obstacle)*obstacle.a*m);
	}

	col.rgb *= _EmissionColorMul;

	return col;//half4(half3((thisPos - centerPos).x*20), 1);//tex2D(_ObstacleTex, thisPos);
}

#endif