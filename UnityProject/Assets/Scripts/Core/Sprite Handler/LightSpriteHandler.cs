using Light2D;
using Logs;
using UnityEngine;

namespace Core.Sprite_Handler
{
	public class LightSpriteHandler  : SpriteHandler
	{
		private LightSprite lightSprite;

		public override Sprite CurrentSprite
		{
			get
			{
				if (lightSprite)
				{
					return lightSprite.Sprite;
				}

				return null;
			}
		}

		public override Color CurrentColor
		{
			get
			{
				if (lightSprite)
				{
					return lightSprite.Color;
				}
				return Color.white;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			lightSprite = GetComponent<LightSprite>();
		}

		protected override void SetImageColor(Color value)
		{
			base.SetImageColor(value);
			if (lightSprite != null)
			{
				lightSprite.Color = value;
			}
		}

		protected override void UpdateImageColor()
		{
			base.UpdateImageColor();
			if (lightSprite != null)
			{
				setColour = lightSprite.Color;
			}
		}

		protected override void SetPaletteOnSpriteRenderer()
		{
			base.SetPaletteOnSpriteRenderer();
			if (lightSprite != null)
			{
				Loggy.LogError("SetPaletteOnSpriteRenderer Is not supported on lightSprite?");
			}
		}

		protected override void SetImageSprite(Sprite value)
		{

			base.SetImageSprite(value);
#if  UNITY_EDITOR
			if (Application.isPlaying == false)
			{
				if (lightSprite == null)
				{
					lightSprite = GetComponent<LightSprite>();
				}

			}
#endif

			if (lightSprite != null)
			{
				lightSprite.Sprite = value;
			}
		}

		protected override bool HasSpriteInImageComponent()
		{
			if (lightSprite != null)
			{
				if (lightSprite.Sprite != null)
				{
					return true;
				}
			}

			return (false);
		}
	}
}
