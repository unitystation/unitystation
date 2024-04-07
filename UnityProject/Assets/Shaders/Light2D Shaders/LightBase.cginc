/*
Base code for standard light shaders.
Light is computed by path tracking with fixed number of steps (PATH_TRACKING_SAMPLES).
*/

#ifndef LIGHT_BASE_INCLUDED
#define LIGHT_BASE_INCLUDED

#pragma glsl_no_auto_normalization

struct light2d_fixed_data_t 
{
	float4 vertex : POSITION;
	float2 texcoord : TEXCOORD0;
	float4 color : COLOR0;
	float2 texcoord1 : TEXCOORD1;
};

struct light2d_fixed_v2f 
{
	float4 vertex : SV_POSITION;
	half2 texcoord : TEXCOORD0;
	half4 color : COLOR0;

	half2 thisPos : TEXCOORD2;
	half2 centerPos : TEXCOORD1;

	half2 aspect : TEXCOORD3;
};
			
uniform sampler2D _FovExtendedMask;
uniform sampler2D _MainTex;
uniform half _EmissionColorMul;
uniform float4 _FovTransformation;

#ifdef UNITY_HALF_TEXEL_OFFSET
uniform half2 _PosOffset;
#endif

light2d_fixed_v2f light2d_fixed_vert (light2d_fixed_data_t v)
{
	light2d_fixed_v2f o;
	o.vertex = UnityObjectToClipPos(v.vertex);
	o.texcoord = v.texcoord;

	float4 vPos = ComputeScreenPos(o.vertex);
	o.thisPos = (vPos.xy/vPos.w - 0.5 + _FovTransformation.xy) * _FovTransformation.zw + 0.5;

	#if LIGHT2D_XY_PLANE
	float4 cPos = ComputeScreenPos(mul(UNITY_MATRIX_VP, float4(v.texcoord1, 0, 1)));
	#elif LIGHT2D_XZ_PLANE
	float4 cPos = ComputeScreenPos(mul(UNITY_MATRIX_VP, float4(v.texcoord1.x, 0, v.texcoord1.y, 1)));
	#endif

	o.centerPos = (cPos.xy/cPos.w - 0.5  + _FovTransformation.xy) * _FovTransformation.zw + 0.5;

	#ifdef UNITY_HALF_TEXEL_OFFSET
	o.thisPos += _PosOffset;
	o.centerPos += _PosOffset;
	#endif

	o.color = v.color;
	o.aspect = half2(_ScreenParams.x/_ScreenParams.y, 1);

	return o;
}

half4 light2_fixed_frag (light2d_fixed_v2f i) : COLOR
{
    half4 tex = tex2D(_MainTex, i.texcoord);
	 
	half2 thisPos = i.thisPos;
	half2 centerPos = i.centerPos;

	half sub = 1.0/PATH_TRACKING_SAMPLES;
		
	half4 colorizedTex = i.color * tex * tex.a * i.color.a;
	half4 col = colorizedTex;

	half pos = 0;

	for(int j = 0; j < PATH_TRACKING_SAMPLES; j++)
	{
		pos += sub; 
		half4 obstacle = tex2D(_FovExtendedMask, lerp(centerPos, thisPos, pos)); 
		col *= 1-obstacle.r;
	}
	
	half4 fov = tex2D(_FovExtendedMask, thisPos);
	col.rgb += colorizedTex * fov.b;
	col.rgb *= fov.g;
	col.rgb *= _EmissionColorMul;

	return col;
}

#endif