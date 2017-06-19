// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Shader created with Shader Forge v1.02 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.02;sub:START;pass:START;ps:flbk:,lico:1,lgpr:1,nrmq:1,limd:0,uamb:True,mssp:True,lmpd:False,lprd:False,rprd:False,enco:False,frtr:True,vitr:True,dbil:False,rmgx:True,rpth:0,hqsc:True,hqlp:False,tesm:0,blpr:0,bsrc:0,bdst:1,culm:0,dpts:6,wrdp:False,ufog:True,aust:False,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,ofsf:0,ofsu:0,f2p0:False;n:type:ShaderForge.SFN_Final,id:5747,x:33040,y:32573,varname:node_5747,prsc:2|emission-1556-OUT;n:type:ShaderForge.SFN_Tex2d,id:1085,x:32144,y:32635,ptovrint:False,ptlb:OverlayTex,ptin:_OverlayTex,varname:node_1085,prsc:2,ntxv:1,isnm:False|UVIN-3852-OUT;n:type:ShaderForge.SFN_Tex2d,id:8181,x:32603,y:32516,ptovrint:False,ptlb:MainTex,ptin:_MainTex,varname:node_1085,prsc:2,ntxv:1,isnm:False;n:type:ShaderForge.SFN_Add,id:3852,x:31901,y:32635,varname:node_3852,prsc:2|A-5333-OUT,B-2106-OUT;n:type:ShaderForge.SFN_Multiply,id:5333,x:31713,y:32573,varname:node_5333,prsc:2|A-897-OUT,B-6001-OUT;n:type:ShaderForge.SFN_Append,id:2106,x:31713,y:32733,varname:node_2106,prsc:2|A-4400-OUT,B-8560-OUT;n:type:ShaderForge.SFN_ComponentMask,id:6001,x:31505,y:32563,varname:node_6001,prsc:2,cc1:0,cc2:1,cc3:-1,cc4:-1|IN-5732-XYZ;n:type:ShaderForge.SFN_ValueProperty,id:897,x:31505,y:32493,ptovrint:False,ptlb:CameraScroll,ptin:_CameraScroll,varname:node_897,prsc:2,glob:False,v1:0.14;n:type:ShaderForge.SFN_ViewPosition,id:5732,x:31239,y:32422,varname:node_5732,prsc:2;n:type:ShaderForge.SFN_Add,id:4400,x:31505,y:32733,varname:node_4400,prsc:2|A-3518-R,B-8853-OUT;n:type:ShaderForge.SFN_Add,id:8560,x:31505,y:32881,varname:node_8560,prsc:2|A-3518-G,B-6327-OUT;n:type:ShaderForge.SFN_Multiply,id:8853,x:31239,y:32736,varname:node_8853,prsc:2|A-5932-TSL,B-5820-OUT;n:type:ShaderForge.SFN_Multiply,id:6327,x:31239,y:32884,varname:node_6327,prsc:2|A-5932-TSL,B-4168-OUT;n:type:ShaderForge.SFN_ComponentMask,id:3518,x:31239,y:32566,varname:node_3518,prsc:2,cc1:0,cc2:1,cc3:-1,cc4:-1|IN-379-OUT;n:type:ShaderForge.SFN_Multiply,id:379,x:31052,y:32496,varname:node_379,prsc:2|A-5187-UVOUT,B-6851-OUT;n:type:ShaderForge.SFN_Time,id:5932,x:30996,y:32746,varname:node_5932,prsc:2;n:type:ShaderForge.SFN_ValueProperty,id:5820,x:30996,y:32904,ptovrint:False,ptlb:XScrollSpeed,ptin:_XScrollSpeed,varname:node_5820,prsc:2,glob:False,v1:0;n:type:ShaderForge.SFN_ValueProperty,id:4168,x:30996,y:32995,ptovrint:False,ptlb:YScrollSpeed,ptin:_YScrollSpeed,varname:node_4168,prsc:2,glob:False,v1:0;n:type:ShaderForge.SFN_ValueProperty,id:6851,x:30819,y:32588,ptovrint:False,ptlb:Scale,ptin:_Scale,varname:node_6851,prsc:2,glob:False,v1:1;n:type:ShaderForge.SFN_ScreenPos,id:5187,x:30819,y:32415,varname:node_5187,prsc:2,sctp:1;n:type:ShaderForge.SFN_Blend,id:973,x:32603,y:32718,varname:node_973,prsc:2,blmd:1,clmp:True|SRC-148-OUT,DST-8181-RGB;n:type:ShaderForge.SFN_ValueProperty,id:3220,x:32144,y:32823,ptovrint:False,ptlb:Intensity,ptin:_Intensity,varname:node_3220,prsc:2,glob:False,v1:1;n:type:ShaderForge.SFN_Multiply,id:148,x:32394,y:32635,varname:node_148,prsc:2|A-1085-RGB,B-6168-OUT;n:type:ShaderForge.SFN_Lerp,id:1556,x:32822,y:32753,varname:node_1556,prsc:2|A-8181-RGB,B-973-OUT,T-6609-OUT;n:type:ShaderForge.SFN_Clamp01,id:6609,x:32603,y:32892,varname:node_6609,prsc:2|IN-3220-OUT;n:type:ShaderForge.SFN_Vector1,id:7168,x:32144,y:32883,varname:node_7168,prsc:2,v1:0;n:type:ShaderForge.SFN_Max,id:6168,x:32394,y:32804,varname:node_6168,prsc:2|A-3220-OUT,B-7168-OUT;proporder:1085-8181-897-5820-4168-6851-3220;pass:END;sub:END;*/

Shader "VLS2D/AddOverlay" {
    Properties {
        _OverlayTex ("OverlayTex", 2D) = "gray" {}
        _MainTex ("MainTex", 2D) = "gray" {}
        _CameraScroll ("CameraScroll", Float ) = 0.14
        _XScrollSpeed ("XScrollSpeed", Float ) = 0
        _YScrollSpeed ("YScrollSpeed", Float ) = 0
        _Scale ("Scale", Float ) = 1
        _Intensity ("Intensity", Float ) = 1
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
            uniform float4 _TimeEditor;
            uniform sampler2D _OverlayTex; uniform float4 _OverlayTex_ST;
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform float _CameraScroll;
            uniform float _XScrollSpeed;
            uniform float _YScrollSpeed;
            uniform float _Scale;
            uniform float _Intensity;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = o.pos;
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                i.screenPos = float4( i.screenPos.xy / i.screenPos.w, 0, 0 );
                i.screenPos.y *= _ProjectionParams.x;
/////// Vectors:
////// Lighting:
////// Emissive:
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                float2 node_3518 = (float2(i.screenPos.x*(_ScreenParams.r/_ScreenParams.g), i.screenPos.y).rg*_Scale).rg;
                float4 node_5932 = _Time + _TimeEditor;
                float2 node_3852 = ((_CameraScroll*_WorldSpaceCameraPos.rg)+float2((node_3518.r+(node_5932.r*_XScrollSpeed)),(node_3518.g+(node_5932.r*_YScrollSpeed))));
                float4 _OverlayTex_var = tex2D(_OverlayTex,TRANSFORM_TEX(node_3852, _OverlayTex));
                float3 emissive = lerp(_MainTex_var.rgb,saturate(((_OverlayTex_var.rgb*max(_Intensity,0.0))*_MainTex_var.rgb)),saturate(_Intensity));
                float3 finalColor = emissive;
                return fixed4(finalColor,1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
