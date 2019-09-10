using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Holds value of what sprite to load
/// sprite-based
[RequireComponent(typeof(Image))]
public class NetSpriteAndColor : NetUIElement
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
				string hexColor = split[2];

				if(spriteOffset == -1)
				{
					GetComponent<Image>().enabled = false;
				}
				else
				{
					GetComponent<Image>().enabled = true;
					var spriteSheet = SpriteManager.PlayerSprites[spriteFile];
					GetComponent<Image>().sprite = spriteSheet[spriteOffset];
					GetComponent<Graphic>().color = DebugTools.HexToColor(hexColor);
				}
			}
			externalChange = false;
		}
	}

	/// <summary>
	/// Sets the value, this function only exists to make the code easier to read
	/// </summary>
	public void SetComplicatedValue(string spriteSheet, int spriteOffset, string hexColor)
	{
		SetValue = $"{spriteSheet}@{spriteOffset.ToString()}@{hexColor}";
	}

	public override void ExecuteServer() { }
}