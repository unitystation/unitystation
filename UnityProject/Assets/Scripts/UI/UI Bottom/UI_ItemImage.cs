using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class manages items sprites rendering for UI Images
/// It creates new Image instances in root gameobject for each sprite render in item
/// </summary>
public class UI_ItemImage
{
	private readonly GameObject root;
	private bool hidden;

	private Stack<ImageAndHandler> usedImages = new Stack<ImageAndHandler>();
	private Stack<ImageAndHandler> freeImages = new Stack<ImageAndHandler>();
	private Image overlay;

	/// <summary>
	/// The first sprite in rendered item
	/// Null if there is no item
	/// </summary>
	public Sprite MainSprite
	{
		get
		{
			if (usedImages.Count != 0)
			{
				var firstImage = usedImages.Peek();
				if (firstImage != null && firstImage.Handler)
				{
					return firstImage.Handler.CurrentSprite;
				}
			}

			return null;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="root">Object to be used as parent for new Image instances</param>
	public UI_ItemImage(GameObject root)
	{
		this.root = root;

		// generate and hide overlay image
		overlay = CreateNewImage("uiItemImageOverlay");
		SetOverlay(null);
	}

	/// <summary>
	/// Disable all sprites, but not reset their value
	/// </summary>
	public void SetHidden(bool hidden)
	{
		this.hidden = hidden;
		foreach (var pair in usedImages)
		{
			pair.UIImage.enabled = !hidden;
			pair.UIImage.preserveAspect = !hidden;
		}
	}

	/// <summary>
	/// Display item as a composition of Image objects in UI
	/// </summary>
	public void ShowItem(GameObject item, Color? forcedColor = null)
	{
		// hide previous image
		ClearAll();
		//determine the sprites to display based on the new item
		var spriteHandlers = item.GetComponentsInChildren<SpriteHandler>(includeInactive: true);
		spriteHandlers = spriteHandlers.Where(x => x.CurrentSprite != null && x != Highlight.instance.spriteRenderer).ToArray();

		foreach (var handler in spriteHandlers)
		{
			// get unused image from stack and subscribe it handler updates
			var image = ConnectFreeImageToHandler(handler);

			// check if handler is hidden
			image.gameObject.SetActive(!handler.IsHiden);

			// set sprite
			var sprite = handler.CurrentSprite;
			image.sprite = sprite;

			// set color
			var color = handler.CurrentColor;
			image.color = color;

			// I don't have any idea what is happening here or how to test it
			// set palleted and color palette
			var itemAttrs = item.GetComponent<ItemAttributesV2>();
			if (itemAttrs.ItemSprites.SpriteInventoryIcon != null && itemAttrs.ItemSprites.IsPaletted)
			{
				image.material.SetInt("_IsPaletted", 1);
				image.material.SetColorArray("_ColorPalette", itemAttrs.ItemSprites.Palette.ToArray());
			}
			else
			{
				image.material.SetInt("_IsPaletted", 0);
			}

			var colorSync = item.GetComponent<SpriteColorSync>();
			if (colorSync != null)
			{   //later find a way to remove this listener when no longer needed
				colorSync.OnColorChange.AddListener(TrackColor);

				void TrackColor(Color newColor)
				{
					if (colorSync.SpriteRenderer != null
						&& colorSync.SpriteRenderer.sprite == image.sprite)
					{
						image.color = newColor;
					}
				}
			}

			bool forceColor = color != null;
			if (forceColor)
			{
				image.color = forcedColor.GetValueOrDefault(Color.white);
			}

			image.enabled = !hidden;
			image.preserveAspect = !hidden;
		}
	}

	/// <summary>
	/// Set overlay image for item (like handcufs icon)
	/// Null to clear sprite and hide image
	/// </summary>
	/// <param name="sprite"></param>
	public void SetOverlay(Sprite overlaySprite)
	{
		if (overlaySprite != null)
		{
			overlay.sprite = overlaySprite;
			overlay.enabled = !hidden;
			overlay.preserveAspect = true;
		}
		else
		{
			overlay.sprite = null;
			overlay.enabled = false;
		}
	}

	/// <summary>
	/// Disable all images and reset their sprites
	/// </summary>
	public void ClearAll()
	{
		while (usedImages.Count != 0)
		{
			var usedImage = usedImages.Pop();
			freeImages.Push(usedImage);

			// reset and hide used image
			usedImage.Handler = null;
			usedImage.UIImage.enabled = false;
		}

		SetOverlay(null);
	}

	private Image ConnectFreeImageToHandler(SpriteHandler handler)
	{
		ImageAndHandler pair;
		if (freeImages.Count > 0)
		{
			pair = freeImages.Pop();
		}
		else
		{
			var img = CreateNewImage();
			pair = new ImageAndHandler(img);
		}

		pair.Handler = handler;
		usedImages.Push(pair);

		return pair.UIImage;
	}

	private Image CreateNewImage(string name = "uiItemImage")
	{
		var go = new GameObject(name, typeof(RectTransform));

		var rt = go.GetComponent<RectTransform>();
		rt.SetParent(root.transform);
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.sizeDelta = Vector2.zero;
		rt.anchoredPosition = Vector2.zero;
		rt.localScale = Vector3.one;

		var img = go.AddComponent<Image>();
		var imgMat = Resources.Load<Material>("Materials/Palettable UI");
		img.material = Object.Instantiate(imgMat);
		img.alphaHitTestMinimumThreshold = 0.5f;

		return img;
	}

	/// <summary>
	/// This class subscribe UIImage to SpriteHandler updates
	/// If SpriteHandler updates sprite this will also update it for UIImage
	/// </summary>
	private class ImageAndHandler
	{
		public Image UIImage { get; private set; }
		private SpriteHandler handler;

		public ImageAndHandler(Image image)
		{
			UIImage = image;
		}

		public SpriteHandler Handler
		{
			get
			{
				return handler;
			}
			set
			{
				// unsubscribe from old handler changes
				if (handler != null)
				{
					handler.OnSpriteChanged -= OnHandlerSpriteChanged;
				}

				handler = value;

				// subscribe to new handler changes
				if (handler)
				{
					handler.OnSpriteChanged += OnHandlerSpriteChanged;
				}
			}
		}

        private void OnHandlerSpriteChanged(Sprite sprite)
		{
			if (!UIImage)
			{
				// looks like image was deleted from scene
				// this happens when item is moved in container
				// and player close this container
				handler.OnSpriteChanged -= OnHandlerSpriteChanged;
				return;
			}

			if (sprite)
			{
				UIImage.gameObject.SetActive (true);
				UIImage.sprite = sprite;
			}
			else
			{
				UIImage.gameObject.SetActive(false);
			}

		}
	}
}