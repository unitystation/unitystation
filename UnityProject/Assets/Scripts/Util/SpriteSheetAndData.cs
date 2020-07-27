using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json;


[System.Serializable]
public class SpriteSheetAndData
{

	public Texture2D Texture;

	[SerializeField]
	public Sprite[] Sprites;
	public TextAsset EquippedData;


	public void setSprites()
	{
		if (Texture != null)
		{
#if UNITY_EDITOR
			var path = AssetDatabase.GetAssetPath(Texture);
			Sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
			if (Sprites.Length > 1)
			{
				Sprites = Sprites.OrderBy(x => int.Parse(x.name.Substring(x.name.LastIndexOf('_') + 1))).ToArray();
			}
			//yeah If you named your sub sprites rip, have to find another way of ordering them correctly since the editor doesnt want to do that		E
			EquippedData = (TextAsset)AssetDatabase.LoadAssetAtPath(path.Replace(".png", ".json"), typeof(TextAsset));
#endif
		}
		else
		{
			Sprites = null;
			EquippedData = null;
		}
	}


	public SpriteData Data
	{
		get
		{
			/*if (data.List.Count == 0)
			{
				if (Texture != null)
				{
					data.List.Add(SpriteFunctions.CompleteSpriteSetup(this));
				}
			}*/
			return data;
		}
		set
		{
			data = value;
		}
	}

	private SpriteData data = new SpriteData();
}