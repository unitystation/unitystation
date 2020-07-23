using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	public class CharacterSprites : MonoBehaviour
	{
		private CharacterDir currentDir = CharacterDir.down;
		//FFGD public List<List<SpriteHandler.SpriteInfo>> sprites = new List<List<SpriteHandler.SpriteInfo>>();

		private int referenceOffset;
		private CharacterView characterView;

		public Image image;

		void Awake()
		{
			image = GetComponent<Image>();
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
			if (image == null)
			{
				// It's possible that UpdateSprite gets called before Awake
				// so grab the image here just in case that happens
				image = GetComponent<Image>();
			}

			//FFGD if (sprites != null && sprites.Count > 0)
			//FFGD {
				//FFGD image.enabled = true;
				//If reference -1 then clear the sprite
				//FFGD if (sprites != null)
				//FFGD {
					//FFGD 	image.sprite = sprites[referenceOffset][0].sprite;
					//FFGD }
				//FFGD 	}
				//FFGD else
				//FFGD 	{
				//FFGD 	image.sprite = null;
				//FFGD 	image.enabled = false;
				//FFGD }
		}

	}
}