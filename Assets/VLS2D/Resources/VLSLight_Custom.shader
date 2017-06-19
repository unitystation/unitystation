// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Shader created with Shader Forge v1.02 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.02;sub:START;pass:START;ps:flbk:,lico:1,lgpr:1,nrmq:1,limd:1,uamb:True,mssp:True,lmpd:False,lprd:False,rprd:False,enco:False,frtr:True,vitr:True,dbil:False,rmgx:True,rpth:0,hqsc:True,hqlp:False,tesm:0,blpr:5,bsrc:3,bdst:0,culm:0,dpts:6,wrdp:False,ufog:False,aust:False,igpj:True,qofs:0,qpre:3,rntp:5,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,ofsf:0,ofsu:0,f2p0:False;n:type:ShaderForge.SFN_Final,id:1,x:33793,y:32843,varname:node_1,prsc:2|emission-2-RGB,alpha-3926-OUT;n:type:ShaderForge.SFN_VertexColor,id:2,x:33274,y:32772,varname:node_2,prsc:2;n:type:ShaderForge.SFN_ComponentMask,id:1231,x:32883,y:33123,varname:node_1231,prsc:2,cc1:0,cc2:1,cc3:-1,cc4:-1|IN-1232-OUT;n:type:ShaderForge.SFN_Multiply,id:1232,x:32697,y:33123,varname:node_1232,prsc:2|A-6870-OUT,B-6870-OUT;n:type:ShaderForge.SFN_TexCoord,id:1265,x:32314,y:33123,varname:node_1265,prsc:2,uv:0;n:type:ShaderForge.SFN_Add,id:596,x:33097,y:33192,varname:node_596,prsc:2|A-1231-R,B-1231-G;n:type:ShaderForge.SFN_RemapRange,id:6870,x:32506,y:33123,varname:node_6870,prsc:2,frmn:0,frmx:1,tomn:-1,tomx:1|IN-1265-UVOUT;n:type:ShaderForge.SFN_Lerp,id:3926,x:33618,y:33103,varname:node_3926,prsc:2|A-8101-OUT,B-3247-OUT,T-4421-OUT;n:type:ShaderForge.SFN_Vector1,id:8101,x:33418,y:33038,varname:node_8101,prsc:2,v1:1;n:type:ShaderForge.SFN_Vector1,id:3247,x:33418,y:33101,varname:node_3247,prsc:2,v1:0;n:type:ShaderForge.SFN_Slider,id:3388,x:32726,y:33319,ptovrint:False,ptlb:Intensity,ptin:_Intensity,varname:node_3388,prsc:2,min:0,cur:1,max:1;n:type:ShaderForge.SFN_Blend,id:4421,x:33295,y:33192,varname:node_4421,prsc:2,blmd:6,clmp:True|SRC-596-OUT,DST-1939-OUT;n:type:ShaderForge.SFN_OneMinus,id:1939,x:33097,y:33319,varname:node_1939,prsc:2|IN-3388-OUT;proporder:3388;pass:END;sub:END;*/

Shader "VLS2D/VLSLight_Custom" {
    Properties {
        _Intensity ("Intensity", Range(0, 1)) = 1
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Overlay"
        }
        Pass {
            Name "ForwardBase"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend SrcAlpha One
            ZTest Always
            ZWrite Off
            
            Fog {Mode Off}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma exclude_renderers xbox360 ps3 flash d3d11_9x 
            #pragma target 3.0
            uniform float _Intensity;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
/////// Vectors:
////// Lighting:
////// Emissive:
                float3 emissive = i.vertexColor.rgb;
                float3 finalColor = emissive;
                float2 node_6870 = (i.uv0*2.0+-1.0);
                float2 node_1231 = (node_6870*node_6870).rg;
                float node_596 = (node_1231.r+node_1231.g);
                float node_4421 = saturate((1.0-(1.0-node_596)*(1.0-(1.0 - _Intensity))));
                return fixed4(finalColor,lerp(1.0,0.0,node_4421));
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
