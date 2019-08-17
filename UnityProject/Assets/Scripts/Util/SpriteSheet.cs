using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;

[System.Serializable]
public class SpriteSheet 
{
	
	public Texture2D Texture;

	public Sprite[] Sprites;

	public void setSprites()
	{
		Logger.Log("WOWo3o");
		if (Texture != null)
		{
			var path = AssetDatabase.GetAssetPath(Texture).Substring(17);//Substring(17) To remove the "Assets/Resources/"
			Sprites = Resources.LoadAll<Sprite>(path.Remove(path.Length - 4));
		}
		else { 
			Sprites = null;
		}
	}
}
