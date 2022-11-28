/*

Light shader with 60 path tracking steps.
Code contained in LightBase.cginc, only path tracking samples count is defined here.

*/

Shader "Light2D/Light Minimal Op" {
Properties {
	_MainTex ("Light texture", 2D) = "white" {}
	_ObstacleMul ("Obstacle Mul", Float) = 500
	_EmissionColorMul ("Emission color mul", Float) = 1
}
SubShader {	
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}

	LOD 100
	//Blend OneMinusDstColor One
	BlendOp Add, Max
	Blend OneMinusDstColor One, OneMinusDstAlpha One
	
	Cull Off
	ZWrite Off
	Lighting Off

	Pass {  
		CGPROGRAM
			#define PATH_TRACKING_SAMPLES 30 // count of path tracking steps
			#pragma target 3.0
			#pragma multi_compile ORTHOGRAPHIC_CAMERA PERSPECTIVE_CAMERA
			#pragma multi_compile LIGHT2D_XY_PLANE LIGHT2D_XZ_PLANE
			
			#include "UnityCG.cginc"
			#include "LightBase.cginc" // all code is here
			
			#pragma vertex light2d_fixed_vert
			#pragma fragment light2_fixed_frag
		ENDCG
	}
}

Fallback "Light2D/Light 30 Points"

}