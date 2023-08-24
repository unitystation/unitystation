Shader "Unitystation/Shader/Vengiate"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainColor("Color", Color) = (1,1,1,1)
        _vignetteColor("vignetteColor", Color) = (1,1,1,1)
        _vignetteSize("VignetteSize", float) = 0.0
        _vignetteIntensity("VignetteIntensity", float) = 0.0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainColor;
            float4 _vignetteColor;
            float _vignetteSize;
            float _vignetteIntensity;
            
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate the distance from the center of the screen
                float2 center = float2(0.5, 0.5);
                float distance = length(i.uv - center);

                // Define the vignette parameters
                float vignetteSize = _vignetteSize; // Adjust this value to control the size of the vignette
                float vignetteIntensity = _vignetteIntensity; // Adjust this value to control the intensity of the vignette
                float4 vignetteColor = _vignetteColor; // Adjust this value to set the vignette color (in this example, red)

                // Apply the vignette effect
                float vignette = smoothstep(vignetteSize, vignetteSize - vignetteIntensity, distance);
                fixed4 color = tex2D(_MainTex, i.uv) * _MainColor;
                color.rgb = lerp(color.rgb, vignetteColor.rgb, vignette);

                return color;
            }
            
            ENDCG
        }
    }
}