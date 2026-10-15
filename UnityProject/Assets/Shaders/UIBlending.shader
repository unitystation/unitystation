Shader "Unitystation/Shader/UIBlending"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _UITexture ("UI Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        // No fixed-function blending: the render graph blit pass can't be relied on to preserve
        // the destination, so the blend is done in the shader from a copy of the scene (_MainTex).
        Cull Off ZWrite Off ZTest Always Blend Off

        Pass
        {
            Name "UIBlending"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);
            TEXTURE2D(_UITexture); SAMPLER(sampler_UITexture);

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                half4 scene = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 ui = SAMPLE_TEXTURE2D(_UITexture, sampler_UITexture, uv);

                // Same result as the old "Blend SrcAlpha OneMinusSrcAlpha" applied to rgb and alpha:
                //   out = ui * ui.a + scene * (1 - ui.a)
                half4 result;
                result.rgb = ui.rgb * ui.a + scene.rgb * (1.0 - ui.a);
                result.a = ui.a * ui.a + scene.a * (1.0 - ui.a);
                return result;
            }
            ENDHLSL
        }
    }
}
