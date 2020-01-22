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
			if (Sprites.Length > 0)
			{
				if (!Sprites[0].name.Contains("_0"))
				{
					Logger.LogWarning("Sprites are Loaded bloody backwards or The first Sprite name in the text Does not contain _0, Fixing");
					Sprites = Sprites.Reverse().ToArray();
				}
			}
			EquippedData = (TextAsset)AssetDatabase.LoadAssetAtPath(path.Replace(".png", ".json"), typeof(TextAsset));
#endif
		}
		else
		{
			Sprites = null;
			EquippedData = null;
		}
	}
}