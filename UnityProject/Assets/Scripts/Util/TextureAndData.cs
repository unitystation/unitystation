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