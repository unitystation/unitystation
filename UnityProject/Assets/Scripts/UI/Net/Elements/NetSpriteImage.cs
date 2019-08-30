using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Holds value of what sprite to load
/// sprite-based
[RequireComponent(typeof(Image))]
public class NetSpriteImage : NetUIElement
{
	public override ElementMode InteractionMode => ElementMode.ServerWrite;
	public static Dictionary<string, Sprite[]> Sprites = new Dictionary<string, Sprite[]>();

	private string cacheValue = "-1";

	public override string Value
	{
		get { return cacheValue ?? "-1"; }
		set
		{
			externalChange = true;

			if (cacheValue != value)
			{
				cacheValue = value;

				var split = value.Split(new[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
				string spriteFile = split[0];
				int spriteOffset = int.Parse(split[1]);

				if (spriteOffset == -1)
				{
					GetComponent<Image>().enabled = false;
				}
				else
				{
					GetComponent<Image>().enabled = true;

					Sprite[] spriteSheet = SpriteManager.PlayerSprites[spriteFile];
					if ( spriteSheet == null )
					{
						spriteSheet = SpriteManager.ScreenUISprites[spriteFile];
					}

					GetComponent<Image>().sprite = spriteSheet[spriteOffset];
				}
			}
			externalChange = false;
		}
	}

	/// <summary>
	/// Sets the value, this function only exists to make the code easier to read
	/// </summary>
	public void SetComplicatedValue(string spriteSheet, int spriteOffset)
	{
		SetValue = $"{spriteSheet}@{spriteOffset.ToString()}";
	}

	public override void ExecuteServer() { }
}