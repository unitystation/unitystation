// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Shader created with Shader Forge v1.02 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.02;sub:START;pass:START;ps:flbk:,lico:1,lgpr:1,nrmq:1,limd:0,uamb:True,mssp:True,lmpd:False,lprd:False,rprd:False,enco:False,frtr:True,vitr:True,dbil:False,rmgx:True,rpth:0,hqsc:True,hqlp:False,tesm:0,blpr:5,bsrc:3,bdst:0,culm:0,dpts:6,wrdp:False,ufog:False,aust:True,igpj:True,qofs:1,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,ofsf:0,ofsu:0,f2p0:False;n:type:ShaderForge.SFN_Final,id:1,x:33851,y:32843,varname:node_1,prsc:2|emission-1183-OUT,alpha-3-A;n:type:ShaderForge.SFN_VertexColor,id:2,x:33332,y:32772,varname:node_2,prsc:2;n:type:ShaderForge.SFN_Tex2d,id:3,x:33265,y:32944,ptovrint:False,ptlb:MainTex,ptin:_MainTex,varname:node_9359,prsc:2,ntxv:0,isnm:False|UVIN-68-UVOUT;n:type:ShaderForge.SFN_Rotator,id:68,x:32901,y:32910,varname:node_68,prsc:2|UVIN-88-UVOUT,SPD-103-OUT;n:type:ShaderForge.SFN_TexCoord,id:88,x:32683,y:32855,varname:node_88,prsc:2,uv:0;n:type:ShaderForge.SFN_ValueProperty,id:103,x:32683,y:33037,ptovrint:False,ptlb:Rotation Speed,ptin:_RotationSpeed,varname:node_5895,prsc:2,glob:False,v1:0;n:type:ShaderForge.SFN_Blend,id:1183,x:33579,y:32888,varname:node_1183,prsc:2,blmd:1,clmp:True|SRC-2-RGB,DST-3-RGB;proporder:3-103;pass:END;sub:END;*/

Shader "VLS2D/VLSLight" {
    Properties {
        _MainTex ("MainTex", 2D) = "white" {}
        _RotationSpeed ("Rotation Speed", Float ) = 0
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent+1"
            "RenderType"="Transparent"
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
            uniform float4 _TimeEditor;
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform float _RotationSpeed;
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
                float4 node_5695 = _Time + _TimeEditor;
                float node_68_ang = node_5695.g;
                float node_68_spd = _RotationSpeed;
                float node_68_cos = cos(node_68_spd*node_68_ang);
                float node_68_sin = sin(node_68_spd*node_68_ang);
                float2 node_68_piv = float2(0.5,0.5);
                float2 node_68 = (mul(i.uv0-node_68_piv,float2x2( node_68_cos, -node_68_sin, node_68_sin, node_68_cos))+node_68_piv);
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(node_68, _MainTex));
                float3 emissive = saturate((i.vertexColor.rgb*_MainTex_var.rgb));
                float3 finalColor = emissive;
                return fixed4(finalColor,_MainTex_var.a);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
