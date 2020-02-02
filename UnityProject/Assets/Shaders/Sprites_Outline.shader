Shader "Sprites/Outline"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		[HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
		[HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
		[PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
		[PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

		_OutlineColor("Outline Color", Color) = (1,1,1,1)
		_InnerOutline("InnerOutline Size", int) = 1
		_OuterOutline("OuterOutline Size", int) = 1
		_Soften("Soften", Range (0, 1)) = 0
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		Pass
		{
		CGPROGRAM
			#pragma vertex SpriteVert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_instancing
			#pragma multi_compile _ PIXELSNAP_ON
			#pragma multi_compile _ ETC1_EXTERNAL_ALPHA
			#include "UnitySprites.cginc"

			float _Outline;
			fixed4 _OutlineColor;
			int _InnerOutline;
			int _OuterOutline;
			float4 _MainTex_TexelSize;
			float _Soften;

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 _color = SampleSpriteTexture(IN.texcoord) * IN.color;

				// Sample Inner outline.
				float _innerAlpha = 1.0;
				
				[unroll(16)]
				for (int i = 1; i < _InnerOutline + 1; i++) {
					fixed4 _up = tex2D(_MainTex, IN.texcoord + fixed2(0, i * _MainTex_TexelSize.y));
					fixed4 _down = tex2D(_MainTex, IN.texcoord - fixed2(0,i *  _MainTex_TexelSize.y));
					fixed4 _right = tex2D(_MainTex, IN.texcoord + fixed2(i * _MainTex_TexelSize.x, 0));
					fixed4 _left = tex2D(_MainTex, IN.texcoord - fixed2(i * _MainTex_TexelSize.x, 0));

					float _sample =  (_up.a * _down.a * _right.a * _left.a) + _Soften;

					_innerAlpha *= _sample; 
				}

				float _innerMask = _color.a - _innerAlpha;


				// Sample Outer outline.
				float _outerAlpha = 0;

				[unroll(16)]
				for (int i = 1; i < _OuterOutline + 1; i++) 
				{
					fixed4 _up = tex2D(_MainTex, IN.texcoord + fixed2(0, i * _MainTex_TexelSize.y));
					fixed4 _down = tex2D(_MainTex, IN.texcoord - fixed2(0,i *  _MainTex_TexelSize.y));
					fixed4 _right = tex2D(_MainTex, IN.texcoord + fixed2(i * _MainTex_TexelSize.x, 0));
					fixed4 _left = tex2D(_MainTex, IN.texcoord - fixed2(i * _MainTex_TexelSize.x, 0));

					float _sample = (_up.a + _down.a + _right.a + _left.a) - _Soften;

					_outerAlpha += _sample;
				}

				float _outerMask = clamp(clamp(_outerAlpha, 0, 1) - _color.a ,0, 1);

				float _mixedOutlineMask = clamp(_outerMask + _innerMask, 0, 1) * _OutlineColor.a;

				return fixed4((_color.rgb * (_color.a - _mixedOutlineMask)) + (_OutlineColor * _mixedOutlineMask), _mixedOutlineMask + _color.a);
			}
		ENDCG
		}
	}
}
