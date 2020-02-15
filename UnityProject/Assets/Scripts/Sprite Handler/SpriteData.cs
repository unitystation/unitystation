using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class SpriteData
{
	public List<List<List<SpriteHandler.SpriteInfo>>> List = new List<List<List<SpriteHandler.SpriteInfo>>>();
	public bool isPaletted = false;
	public List<Color> palette = new List<Color>(new Color[8]);

	public bool HasSprite()
	{

		if (List.Count > 0 &&
			List[0].Count > 0 &&
			List[0][0].Count > 0)
		{
			return (true);
		}
		else {
			return (false);
		}
	}

	public Sprite ReturnFirstSprite()
	{
		if (List.Count > 0 &&
			List[0].Count > 0 &&
			List[0][0].Count > 0)
		{
			return (List[0][0][0].sprite);
		}
		else {
			return (null);
		}
	}
}