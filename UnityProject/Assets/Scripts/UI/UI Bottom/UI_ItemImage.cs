using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UI_ItemImage
{
	private readonly GameObject root;
	private bool hidden;

	private Stack<Image> usedImages = new Stack<Image>();
	private Stack<Image> freeImages = new Stack<Image>();

	public Sprite MainSprite
	{
		get
		{
			if (usedImages.Count != 0)
			{
				var firstImage = usedImages.Peek();
				if (firstImage)
				{
					return firstImage.sprite;
				}
			}

			return null;
		}
	}

	public UI_ItemImage(GameObject root)
	{
		this.root = root;
	}

	public void SetHidden(bool hidden)
	{
		this.hidden = hidden;
		foreach (var image in usedImages)
		{
			image.enabled = !hidden;
			image.preserveAspect = !hidden;
		}
	}

	public void ShowItem(GameObject item, Color? forcedColor = null)
	{
		// hide previous image
		ClearAll();
		//determine the sprites to display based on the new item
		var spriteRends = item.GetComponentsInChildren<SpriteRenderer>();
		spriteRends = spriteRends.Where(x => x.sprite != null && x != Highlight.instance.spriteRenderer).ToArray();

		foreach (var render in spriteRends)
		{
			var image = GetFreeImage();

			// set sprite
			var sprite = render.sprite;
			image.sprite = sprite;

			// set color
			var color = image.color;
			image.color = color;

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

	public void ClearAll()
	{
		while (usedImages.Count != 0)
		{
			var img = usedImages.Pop();
			freeImages.Push(img);
			img.enabled = false;
		}
	}

	private Image GetFreeImage()
	{
		Image ret;
		if (freeImages.Count > 0)
		{
			ret = freeImages.Pop();
		}
		else
		{
			ret = CreateNewImage();
		}

		usedImages.Push(ret);
		return ret;
	}

	private Image CreateNewImage()
	{
		var go = new GameObject("image", typeof(RectTransform));

		var rt = go.GetComponent<RectTransform>();
		rt.SetParent(root.transform);
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.sizeDelta = Vector2.zero;
		rt.anchoredPosition = Vector2.zero;

		var img = go.AddComponent<Image>();
		var imgMat = Resources.Load<Material>("Materials/Palettable UI");
		img.material = imgMat;
		img.alphaHitTestMinimumThreshold = 0.5f;

		return img;
	}
}