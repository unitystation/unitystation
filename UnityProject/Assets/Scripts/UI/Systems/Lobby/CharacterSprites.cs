using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CharacterCreator
{
	public class CharacterSprites : MonoBehaviour
	{
		private CharacterCustomization.CharacterDir currentDir = CharacterCustomization.CharacterDir.down;
		public SpriteHandler sprites = null;

		private int referenceOffset;
		private CharacterView characterView;

		public Image image;

		private void Awake()
		{
			sprites = GetComponent<SpriteHandler>();
			if(!sprites)
				Loggy.LogWarning("SpriteHandler component is missing!", Category.Sprites);
		}

		private void Start()
		{
			UpdateSprite();
		}

		private void OnEnable()
		{
			characterView = GetComponentInParent<CharacterView>();
			characterView.dirChangeEvent.AddListener(OnDirChange);
		}

		private void OnDisable()
		{
			characterView.dirChangeEvent.RemoveListener(OnDirChange);
		}

		private void OnDestroy()
		{
			characterView.dirChangeEvent.RemoveListener(OnDirChange);
		}
		public void OnDirChange()
		{
			currentDir = characterView.currentDir;
			UpdateReferenceOffset();
		}

		private void UpdateReferenceOffset()
		{
			if (currentDir == CharacterCustomization.CharacterDir.down)
			{
				referenceOffset = 0;
			}
			if (currentDir == CharacterCustomization.CharacterDir.up)
			{
				referenceOffset = 1;
			}
			if (currentDir == CharacterCustomization.CharacterDir.right)
			{
				referenceOffset = 2;
			}
			if (currentDir == CharacterCustomization.CharacterDir.left)
			{
				referenceOffset = 3;
			}

			UpdateSprite();
		}

		public void UpdateSprite()
		{
			// It's possible that UpdateSprite gets called before Awake
			// so try to grab the image here just in case that happens
			if(sprites != null || TryGetComponent(out sprites))
				sprites.ChangeSpriteVariant(referenceOffset , networked:false);
		}
	}
}
