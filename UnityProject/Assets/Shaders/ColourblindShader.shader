Shader "Custom/Colourblind"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TRITAN ("Int", int) = 0
        _PROTAN ("Int", int) = 0
        _DEUNTAN("Int", int) = 0
    }
    SubShader
    {
    	Pass
		{
        CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile DUMMY PIXELSNAP_ON
			#pragma target 2.0
			#include "UnityCG.cginc"
			sampler2D _MainTex;
			uniform int _TRITAN;
            uniform int _PROTAN;
            uniform int _DEUNTAN;
            
			struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };
 
            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                half2 texcoord  : TEXCOORD0;
            };

			v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }
        

			fixed4 frag(v2f IN) : COLOR
            {
            //float4 pixel_shader(float4 vertex:SV_POSITION) : COLOR
            //{
                half2 offset0 = IN.texcoord;
                // Flip sampling of the Texture if DirectX
                //
                

                float4 colour = tex2D(_MainTex, offset0 );
                // RGB to LMS matrix conversion
                float3 L = float3(0,0,0);
                float3 M = float3(0,0,0);
                float3 S = float3(0,0,0);
                float l = 0.0f;
                float m = 0.0f;
                float s = 0.0f;
                // Simulate color blindness
                if (_TRITAN == 1) // Tritan
                {

                    L = (17.8824f * colour.r) + (43.5161f * colour.g) + (4.11935f * colour.b);
                    M = (3.45565f * colour.r) + (27.1554f * colour.g) + (3.86714f * colour.b);
                    S = (0.0299566f * colour.r) + (0.184309f * colour.g) + (1.46709f * colour.b);
                
                    l = 1.0f * L + 0.0f * M + 0.0f * S;
                    m = 0.0f * L + 1.0f * M + 0.0f * S;
                    s = -0.01224491f * L + 0.07203435f * M + 0.0f * S;

                    colour.r = (0.0809444479f * l) + (-0.130504409f * m) + (0.116721066f * s);
                    colour.g = (-0.0102485335f * l) + (0.0540193266f * m) + (-0.113614708f * s);
                    colour.b = (-0.000365296938f * l) + (-0.00412161469f * m) + (0.693511405f * s);
                    
                }
                
                if (_PROTAN == 1)  //protan 
                {

                    L = (17.8824f * colour.r) + (43.5161f * colour.g) + (4.11935f * colour.b);
                    M = (3.45565f * colour.r) + (27.1554f * colour.g) + (3.86714f * colour.b);
                    S = (0.0299566f * colour.r) + (0.184309f * colour.g) + (1.46709f * colour.b);
                    l = 0.0f * L +  2.27376148f * M + -5.92721645 * S;
                    m = 0.0f * L + 1.0f * M + 0.0f * S;
                    s = 0.0f * L + 0.0f * M + 1.0f * S;

                    colour.r = (0.0809444479f * l) + (-0.130504409f * m) + (0.116721066f * s);
                    colour.g = (-0.0102485335f * l) + (0.0540193266f * m) + (-0.113614708f * s);
                    colour.b = (-0.000365296938f * l) + (-0.00412161469f * m) + (0.693511405f * s);
                }

                if (_DEUNTAN == 1)  // deuntan
                {

                    L = (17.8824f * colour.r) + (43.5161f * colour.g) + (4.11935f * colour.b);
                    M = (3.45565f * colour.r) + (27.1554f * colour.g) + (3.86714f * colour.b);
                    S = (0.0299566f * colour.r) + (0.184309f * colour.g) + (1.46709f * colour.b);
                    
                    l = 1.0f * L + 0.0f * M + 0.0f * S;
                    m = 0.494207f * L + 0.0f * M + 1.24827f * S;
                    s = 0.0f * L + 0.0f * M + 1.0f * S;

                    colour.r = (0.0809444479f * l) + (-0.130504409f * m) + (0.116721066f * s);
                    colour.g = (-0.0102485335f * l) + (0.0540193266f * m) + (-0.113614708f * s);
                    colour.b = (-0.000365296938f * l) + (-0.00412161469f * m) + (0.693511405f * s);
                }
                
                return colour;
            }
			
			ENDCG
		}
    }
}
