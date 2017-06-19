// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Shader created with Shader Forge v1.02 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.02;sub:START;pass:START;ps:flbk:,lico:1,lgpr:1,nrmq:1,limd:0,uamb:False,mssp:True,lmpd:False,lprd:False,rprd:False,enco:False,frtr:True,vitr:True,dbil:False,rmgx:True,rpth:0,hqsc:True,hqlp:False,tesm:0,blpr:1,bsrc:3,bdst:7,culm:0,dpts:6,wrdp:False,ufog:True,aust:True,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,ofsf:0,ofsu:0,f2p0:False;n:type:ShaderForge.SFN_Final,id:1,x:33296,y:32710,varname:node_1,prsc:2|emission-9740-OUT,custl-8926-OUT,alpha-6-A;n:type:ShaderForge.SFN_ValueProperty,id:5,x:32088,y:33170,ptovrint:False,ptlb:Intensity,ptin:_Intensity,varname:node_5756,prsc:2,glob:False,v1:1;n:type:ShaderForge.SFN_Tex2d,id:6,x:32088,y:32589,ptovrint:False,ptlb:MainTex,ptin:_MainTex,varname:node_5650,prsc:2,ntxv:1,isnm:False;n:type:ShaderForge.SFN_Tex2d,id:9,x:32088,y:32792,ptovrint:False,ptlb:OverlayTex,ptin:_OverlayTex,varname:node_2208,prsc:2,ntxv:1,isnm:False;n:type:ShaderForge.SFN_Color,id:951,x:32088,y:32977,ptovrint:False,ptlb:AmbientColor,ptin:_AmbientColor,varname:node_1193,prsc:2,glob:False,c1:0.5,c2:0.5,c3:0.5,c4:1;n:type:ShaderForge.SFN_Multiply,id:9740,x:32698,y:32679,varname:node_9740,prsc:2|A-6-RGB,B-951-RGB;n:type:ShaderForge.SFN_Multiply,id:8926,x:32872,y:32979,varname:node_8926,prsc:2|A-5058-OUT,B-5-OUT;n:type:ShaderForge.SFN_Blend,id:5058,x:32491,y:32921,varname:node_5058,prsc:2,blmd:1,clmp:False|SRC-6-RGB,DST-9-RGB;proporder:5-6-9-951;pass:END;sub:END;*/

Shader "VLS2D/Composite" {
    Properties {
        _Intensity ("Intensity", Float ) = 1
        _MainTex ("MainTex", 2D) = "gray" {}
        _OverlayTex ("OverlayTex", 2D) = "gray" {}
        _AmbientColor ("AmbientColor", Color) = (0.5,0.5,0.5,1)
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Pass {
            Name "ForwardBase"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest Always
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma exclude_renderers xbox360 ps3 flash d3d11_9x 
            #pragma target 3.0
            uniform float _Intensity;
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform sampler2D _OverlayTex; uniform float4 _OverlayTex_ST;
            uniform float4 _AmbientColor;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
/////// Vectors:
////// Lighting:
////// Emissive:
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                float3 emissive = (_MainTex_var.rgb*_AmbientColor.rgb);
                float4 _OverlayTex_var = tex2D(_OverlayTex,TRANSFORM_TEX(i.uv0, _OverlayTex));
                float3 finalColor = emissive + ((_MainTex_var.rgb*_OverlayTex_var.rgb)*_Intensity);
                return fixed4(finalColor,_MainTex_var.a);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
