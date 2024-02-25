// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/RadarGreen" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader {
        Pass {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            sampler2D _SecondTex; 

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : COLOR
            {
                fixed4 color = tex2D(_MainTex, i.uv); // Corrected line here
                fixed4 newColor = fixed4(0.0, 10.0, 0.0, 1.0); // Solid green color

                fixed4 OldFrameColour = tex2D(_SecondTex, i.uv);
                
                // Radar-like effect
                float distance = length(color.rgb - newColor.rgb); // Corrected line here
                newColor.a = smoothstep(0.0, 0.5, distance);
                float time = _Time.y*3.0;
                // Movement ghosts
                color.rgb = color.rgb + (OldFrameColour.rgb );

                //return  OldFrameColour;
                //return color 
                return (color + (OldFrameColour * 0.90)) * newColor;
            }
            ENDCG
        }
    }
}