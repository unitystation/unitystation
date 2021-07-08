using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Holds value of what sprite to load
/// sprite-based
[RequireComponent(typeof(Image))]
public class NetSpriteImage : NetUIStringElement
{
	[Tooltip("The sprites this networked image can choose from.")]
	[SerializeField]
	private Sprite[] sprites = default;

	private Image element;
	public Image Element
	{
		get
		{
			if (!element)
			{
				element = GetComponent<Image>();
			}
			return element;
		}
	}

	private int spriteIndex;

	public override ElementMode InteractionMode => ElementMode.ServerWrite;

	public override string Value
	{
		get { return spriteIndex.ToString() ?? "-1"; }
		set
		{
			externalChange = true;
			SetSprite(value);
			externalChange = false;
		}
	}

	public void SetSprite(int spriteIndex)
	{
		SetValueServer(spriteIndex.ToString());
	}

	private void SetSprite(string sprite)
	{
		if (!int.TryParse(sprite, out int spriteIndex)) return;
		if (spriteIndex > sprites.Length) return;
		if (Element.sprite == sprites[spriteIndex]) return;

		Element.sprite = sprites[spriteIndex];
		this.spriteIndex = spriteIndex;
	}
}
