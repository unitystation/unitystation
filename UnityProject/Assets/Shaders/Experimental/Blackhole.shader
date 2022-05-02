// Shader created with Shader Forge v1.38 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.38;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:0,bdst:1,dpts:2,wrdp:False,dith:0,atcv:False,rfrpo:True,rfrpn:Refraction,coma:15,ufog:False,aust:True,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,atwp:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False,fsmp:False;n:type:ShaderForge.SFN_Final,id:3138,x:33108,y:32711,varname:node_3138,prsc:2|emission-7854-OUT;n:type:ShaderForge.SFN_Fresnel,id:7089,x:32186,y:32534,varname:node_7089,prsc:2|EXP-8836-OUT;n:type:ShaderForge.SFN_SceneColor,id:3723,x:32755,y:32845,varname:node_3723,prsc:2|UVIN-7447-OUT;n:type:ShaderForge.SFN_ScreenPos,id:1467,x:31994,y:32845,varname:node_1467,prsc:2,sctp:2;n:type:ShaderForge.SFN_RemapRange,id:1704,x:32183,y:32845,cmnt:Distortion UVs,varname:node_1704,prsc:2,frmn:0,frmx:1,tomn:1,tomx:-1|IN-1467-UVOUT;n:type:ShaderForge.SFN_NormalVector,id:1295,x:31511,y:32684,cmnt:Old way of calculating distortion.,prsc:2,pt:False;n:type:ShaderForge.SFN_Negate,id:2161,x:31683,y:32684,varname:node_2161,prsc:2|IN-1295-OUT;n:type:ShaderForge.SFN_Transform,id:241,x:31850,y:32684,varname:node_241,prsc:2,tffrom:1,tfto:3|IN-2161-OUT;n:type:ShaderForge.SFN_ComponentMask,id:6330,x:32014,y:32684,varname:node_6330,prsc:2,cc1:0,cc2:1,cc3:-1,cc4:-1|IN-241-XYZ;n:type:ShaderForge.SFN_Add,id:7447,x:32533,y:32845,cmnt:Distort original UVs,varname:node_7447,prsc:2|A-9003-OUT,B-1579-OUT;n:type:ShaderForge.SFN_Multiply,id:9003,x:32350,y:32845,cmnt:Distortion Amount,varname:node_9003,prsc:2|A-4918-OUT,B-1704-OUT;n:type:ShaderForge.SFN_Slider,id:8836,x:31791,y:32529,ptovrint:False,ptlb:Distortion Strength,ptin:_DistortionStrength,varname:node_8836,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:3.020896,max:10;n:type:ShaderForge.SFN_OneMinus,id:3969,x:32360,y:32534,varname:node_3969,prsc:2|IN-7089-OUT;n:type:ShaderForge.SFN_Power,id:4918,x:32535,y:32534,varname:node_4918,prsc:2|VAL-3969-OUT,EXP-4038-OUT;n:type:ShaderForge.SFN_Vector1,id:4038,x:32535,y:32458,cmnt:This is an arbitrary value. You can modify it if you want.,varname:node_4038,prsc:2,v1:6;n:type:ShaderForge.SFN_Smoothstep,id:1841,x:32437,y:32133,cmnt:Create the hole mask,varname:node_1841,prsc:2|A-8502-OUT,B-8424-OUT,V-2665-OUT;n:type:ShaderForge.SFN_Slider,id:1077,x:31531,y:32109,ptovrint:False,ptlb:Hole Size,ptin:_HoleSize,varname:node_1077,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0.7030833,max:1;n:type:ShaderForge.SFN_Add,id:8502,x:32054,y:32250,varname:node_8502,prsc:2|A-9892-OUT,B-9958-OUT;n:type:ShaderForge.SFN_Multiply,id:7854,x:32851,y:32666,cmnt:Combine the mask and distortion,varname:node_7854,prsc:2|A-1841-OUT,B-3723-RGB;n:type:ShaderForge.SFN_Relay,id:1579,x:32429,y:33001,varname:node_1579,prsc:2|IN-3973-OUT;n:type:ShaderForge.SFN_Relay,id:3973,x:32183,y:33001,varname:node_3973,prsc:2|IN-1467-UVOUT;n:type:ShaderForge.SFN_RemapRange,id:9892,x:31872,y:32107,varname:node_9892,prsc:2,frmn:0,frmx:1,tomn:1,tomx:0|IN-1077-OUT;n:type:ShaderForge.SFN_Slider,id:9958,x:31531,y:32332,ptovrint:False,ptlb:Hole Edge Smoothness,ptin:_HoleEdgeSmoothness,cmnt:Maybe this can be controlled by distance?,varname:node_9958,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0.001,cur:0.007289694,max:0.05;n:type:ShaderForge.SFN_Subtract,id:8424,x:32054,y:32107,varname:node_8424,prsc:2|A-9892-OUT,B-9958-OUT;n:type:ShaderForge.SFN_Fresnel,id:7652,x:32256,y:32281,varname:node_7652,prsc:2|EXP-7073-OUT;n:type:ShaderForge.SFN_OneMinus,id:2665,x:32437,y:32281,varname:node_2665,prsc:2|IN-7652-OUT;n:type:ShaderForge.SFN_Vector1,id:7073,x:32256,y:32407,varname:node_7073,prsc:2,v1:0.15;proporder:8836-1077-9958;pass:END;sub:END;*/

Shader "MAG/Blackhole" {
    Properties {
        _DistortionStrength ("Distortion Strength", Range(0, 10)) = 3.020896
        _HoleSize ("Hole Size", Range(0, 1)) = 0.7030833
        _HoleEdgeSmoothness ("Hole Edge Smoothness", Range(0.001, 0.05)) = 0.007289694
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        GrabPass{ }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma only_renderers d3d9 d3d11 glcore gles 
            #pragma target 3.0
            uniform sampler2D _GrabTexture;
            uniform float _DistortionStrength;
            uniform float _HoleSize;
            uniform float _HoleEdgeSmoothness;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 posWorld : TEXCOORD0;
                float3 normalDir : TEXCOORD1;
                float4 projPos : TEXCOORD2;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos( v.vertex );
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalDirection = i.normalDir;
                float2 sceneUVs = (i.projPos.xy);
                float4 sceneColor = tex2D(_GrabTexture, sceneUVs);
////// Lighting:
////// Emissive:
                float node_9892 = (_HoleSize*-1.0+1.0);
                float node_1841 = smoothstep( (node_9892+_HoleEdgeSmoothness), (node_9892-_HoleEdgeSmoothness), (1.0 - pow(1.0-max(0,dot(normalDirection, viewDirection)),0.15)) ); // Create the hole mask
                float node_3969 = (1.0 - pow(1.0-max(0,dot(normalDirection, viewDirection)),_DistortionStrength));
                float3 emissive = (node_1841*tex2D( _GrabTexture, ((pow(node_3969,6.0)*(sceneUVs.rg*-2.0+1.0))+sceneUVs.rg)).rgb);
                float3 finalColor = emissive;
                return fixed4(finalColor,1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
