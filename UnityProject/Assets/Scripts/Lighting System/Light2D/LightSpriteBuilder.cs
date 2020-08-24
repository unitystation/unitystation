namespace Light2D
{
	using UnityEngine;

	public static class LightSpriteBuilder
	{
		private const string MaterialLocation = "Effects/Light2D/Default Light Material";
		private const string SpriteLocation = "Effects/Light2D/Default Light Sprite"; //Effects/Light2D/Default Light Sprite

		private static readonly Color DefaultColor = new Color(1,1,1, 0.6f);

		private static Material mMaterial;
		private static Sprite mSprite;

		private static Material material
		{
			get
			{
				if (mMaterial == null)
				{
					mMaterial = (Material)Resources.Load(MaterialLocation, typeof(Material));
				}

				return mMaterial;
			}
		}

		private static Sprite sprite
		{
			get
			{
				if (mSprite == null)
				{
					mSprite = (Sprite)Resources.Load(SpriteLocation, typeof(Sprite));
				}

				return mSprite;
			}
		}

		public static GameObject BuildDefault(GameObject iRoot, Color iColor = default(Color), float iSize = 7)
		{
			var _gameObject = new GameObject("Light2D");
			_gameObject.transform.parent = iRoot.transform;
			_gameObject.transform.localPosition = Vector3.zero;
			_gameObject.transform.localScale = Vector3.one * iSize;
			_gameObject.layer = 21;
			_gameObject.SetActive(false);

			var _lightSprite = _gameObject.AddComponent<LightSprite>();
			_lightSprite.Material = material;
			_lightSprite.Sprite = sprite;
			_lightSprite.Color = iColor;

			return _gameObject;
		}
	}
}