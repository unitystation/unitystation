using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Light2D
{
	/// <summary>
	///     Sprite with dual color support. Grabs sprite from GameSpriteRenderer field.
	/// </summary>
	[ExecuteInEditMode]
	public class LightObstacleSprite : CustomSprite
	{
		private CustomSprite _oldCustomSprite;
		private Renderer _oldGameSpriteRenderer;

		private Color _oldSecondaryColor;
		private SpriteRenderer _oldUnitySprite;

		/// <summary>
		///     Color is packed in mesh UV1.
		/// </summary>
		public Color AdditiveColor;

		/// <summary>
		///     Renderer from which sprite will be used.
		/// </summary>
		public Renderer GameSpriteRenderer;

		protected override void OnEnable()
		{
#if UNITY_EDITOR
			if (Material == null)
			{
				Material = (Material) AssetDatabase.LoadAssetAtPath("Assets/Light2D/Materials/DualColor.mat", typeof(Material));
			}
#endif

			base.OnEnable();

			if (GameSpriteRenderer == null && transform.parent != null)
			{
				GameSpriteRenderer = transform.parent.gameObject.GetComponent<Renderer>();
			}

			gameObject.layer = LightingSystem.Instance.LightObstaclesLayer;

			UpdateMeshData(true);
		}

		private void UpdateSecondaryColor()
		{
			Vector2 uv1 = new Vector2(Util.DecodeFloatRGBA((Vector4) AdditiveColor), Util.DecodeFloatRGBA(new Vector4(AdditiveColor.a, 0, 0)));
			for (int i = 0; i < _uv1.Length; i++)
			{
				_uv1[i] = uv1;
			}
		}

		protected override void UpdateMeshData(bool forceUpdate = false)
		{
			if (_meshRenderer == null || _meshFilter == null || IsPartOfStaticBatch || Material == null)
			{
				return;
			}

			if (GameSpriteRenderer != null && (GameSpriteRenderer != _oldGameSpriteRenderer || forceUpdate ||
			                                   _oldUnitySprite != null && _oldUnitySprite.sprite != null && _oldUnitySprite.sprite != Sprite ||
			                                   _oldCustomSprite != null && _oldCustomSprite.Sprite != null && _oldCustomSprite.Sprite != Sprite))
			{
				_oldGameSpriteRenderer = GameSpriteRenderer;

				_oldCustomSprite = GameSpriteRenderer.GetComponent<CustomSprite>();
				if (_oldCustomSprite != null)
				{
					Sprite = _oldCustomSprite.Sprite;
				}
				else
				{
					_oldUnitySprite = GameSpriteRenderer.GetComponent<SpriteRenderer>();
					if (_oldUnitySprite != null)
					{
						Sprite = _oldUnitySprite.sprite;
					}
				}

				Material.EnableKeyword("NORMAL_TEXCOORD");
			}

			if (_oldSecondaryColor != AdditiveColor || forceUpdate)
			{
				UpdateSecondaryColor();
				_isMeshDirty = true;
				_oldSecondaryColor = AdditiveColor;
			}

			base.UpdateMeshData(forceUpdate);
		}
	}
}