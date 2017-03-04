/*

This is most expensive light shader with variable count of path tracking steps.
That shader could be used only on desktop / consoles due to poor performance and required features.

*/


Shader "Light2D/Light Auto Points" {
Properties {
    _MainTex ("Light texture", 2D) = "white" {}
    _ObstacleMul ("Obstacle Mul", Float) = 500
    _EmissionColorMul ("Emission color mul", Float) = 1
    _StepCountMul ("Raytracking point count multiplier", Float) = 2
}
SubShader {	
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}

    LOD 100
    Blend OneMinusDstColor One
    Cull Off
    ZWrite Off
    Lighting Off

    Pass {  
        CGPROGRAM
        #pragma target 3.0 // replace with 4.0 if you see glitches with big _StepCountMul or texture resolution.
		#pragma multi_compile ORTHOGRAPHIC_CAMERA PERSPECTIVE_CAMERA
        #pragma glsl_no_auto_normalization
		#pragma glsl
            
        #include "UnityCG.cginc"
		#include "Assets/Light2D/Resources/Shaders/LightBase.cginc" 
			
		#pragma vertex light2d_fixed_vert
        #pragma fragment frag
            
        uniform half _StepCountMul;

        half4 frag (light2d_fixed_v2f i) : COLOR
        {
            half4 tex = tex2D(_MainTex, i.texcoord);
				
			#ifdef PERSPECTIVE_CAMERA
			half4 vPos = ComputeScreenPos(i.projVertex);
			half2 thisPos = (vPos.xy/vPos.w - 0.5)*_ExtendedToSmallTextureScale + 0.5;
			half4 cPos = ComputeScreenPos(mul(UNITY_MATRIX_VP, float4(i.texcoord1, 0, 1)));
			half2 centerPos = (cPos.xy/cPos.w - 0.5)*_ExtendedToSmallTextureScale + 0.5;
			#ifdef UNITY_HALF_TEXEL_OFFSET
			thisPos += _PosOffset;
			centerPos += _PosOffset;
			#endif
			#else
			half2 thisPos = i.thisPos;
			half2 centerPos = i.centerPos;
			#endif
				
            half pixelSize = 1.0/_ScreenParams.y;
			half len = length((thisPos - centerPos)*i.aspect);
            int steps = round(_StepCountMul * len / pixelSize);

			half sub = 1.0/steps;
			half m = _ObstacleMul*sub*len;
			
			half4 col = i.color*tex*tex.a*i.color.a;

			half pos = 0;
	

			for(half i = 0; i < steps; i++)
			{
				pos += sub; 
				half4 obstacle = tex2Dlod(_ObstacleTex, half4(lerp(centerPos, thisPos, pos), 0, 0));
				col *= saturate(1 - (1 - obstacle)*obstacle.a*m);
			}

			col.rgb *= _EmissionColorMul;

			return col;
        }
        ENDCG
    }
}

Fallback "Light2D/Light 80 Points"

}