using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	public class CharacterSprites : MonoBehaviour
	{
		private CharacterDir currentDir = CharacterDir.down;
		public SpriteHandler sprites = null;

		private int referenceOffset;
		private CharacterView characterView;

		public Image image;

		void Awake()
		{
			sprites = GetComponent<SpriteHandler>();
		}
		private void Start()
		{
			UpdateSprite();
		}

		void OnEnable()
		{
			characterView = GetComponentInParent<CharacterView>();
			characterView.dirChangeEvent.AddListener(OnDirChange);
		}

		void OnDisable()
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
			if (currentDir == CharacterDir.down)
			{
				referenceOffset = 0;
			}
			if (currentDir == CharacterDir.up)
			{
				referenceOffset = 1;
			}
			if (currentDir == CharacterDir.right)
			{
				referenceOffset = 2;
			}
			if (currentDir == CharacterDir.left)
			{
				referenceOffset = 3;
			}

			UpdateSprite();
		}

		public void UpdateSprite()
		{
			if (sprites == null)
			{
				// It's possible that UpdateSprite gets called before Awake
				// so grab the image here just in case that happens
				sprites = GetComponent<SpriteHandler>();
			}

			sprites.ChangeSpriteVariant(referenceOffset , NetWork:false);
		}

	}
}