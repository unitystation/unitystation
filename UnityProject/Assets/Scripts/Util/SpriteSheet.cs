using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
/// Warning! due to limitations on unity, You have to manually call setSprites on in OnValidate() In your script 
/// Would be nice if I could do it automatically but thats unity being bs 
///</summary> 
[System.Serializable]
public class SpriteSheet
{
	public Texture2D Texture;

	[SerializeField]
	public Sprite[] Sprites;

	public void setSprites()
	{

		if (Texture != null)
		{
			#if UNITY_EDITOR
			var path = AssetDatabase.GetAssetPath(Texture).Substring(17);//Substring(17) To remove the "Assets/Resources/"
			Sprites = Resources.LoadAll<Sprite>(path.Remove(path.Length - 4));
			#endif
		}
		else {
			Sprites = null;
		}
	}
}
