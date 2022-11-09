Shader "Custom/BadEyesightShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurryStrength ("Int", Int) = 20
    	_COLORBLIND_MODE ("Int", Int) = 1
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
			uniform int _BlurryStrength;
			uniform int _COLORBLIND_MODE;
            float4 _MainTex_TexelSize;

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
				float2 offset0 = IN.texcoord;
				float4 colour = tex2D(_MainTex,offset0);
				float4 addColour = float4(0,0,0,0);

				//20 , Square looks good
				
				float Strength = clamp(( length(float2(0.5,0.5) - offset0)*4) - 0.05, 0, 1);  //TODO Change to pixel Due to Weird screen stretching
            	
				int NumberOfAdditions = 4;
				for(float i=0; i<_BlurryStrength; i++)
				{
					//int2(0,1) + int2(1,-1)  *
					addColour += tex2D(_MainTex,(offset0 + float2 (_MainTex_TexelSize.x,0)*i));//* (1-(i / _BlurryStrength));
					addColour += tex2D(_MainTex,(offset0+ float2 (0,_MainTex_TexelSize.y)*i));// * (1-(i / _BlurryStrength)) ;
					addColour += tex2D(_MainTex,(offset0+ float2 (-_MainTex_TexelSize.x,0)*i));// * (1-(i / _BlurryStrength)) ;
					addColour += tex2D(_MainTex,(offset0 + float2 (0,-_MainTex_TexelSize.y)*i));// * (1-(i / _BlurryStrength)) ;

					//addColour += tex2D(_MainTex, (offset0 + float2 (-_MainTex_TexelSize.x,-_MainTex_TexelSize.y)*i) ) * (1-(i / _BlurryStrength)) ;
					//addColour += tex2D(_MainTex, (offset0 + float2 (_MainTex_TexelSize.x,-_MainTex_TexelSize.y)*i)   ) * (1-(i / _BlurryStrength)) ;
					//addColour += tex2D(_MainTex, (offset0 + float2 (-_MainTex_TexelSize.x,_MainTex_TexelSize.y)*i)  ) * (1-(i / _BlurryStrength)) ;
					//addColour += tex2D(_MainTex, (offset0 + float2 (_MainTex_TexelSize.x,_MainTex_TexelSize.y)*i)   ) * (1-(i / _BlurryStrength)) ;
				}
 
            	
				
				//return addColour / (float4 (1,1,1,1) * BlurStrength * NumberOfAdditions);
				return lerp(colour, addColour / ((float4 (1,1,1,1) * _BlurryStrength * NumberOfAdditions)), float4(Strength,Strength,Strength,Strength));
				return float4(Strength,Strength,Strength,Strength);
			}
			ENDCG
		}
    }
}
