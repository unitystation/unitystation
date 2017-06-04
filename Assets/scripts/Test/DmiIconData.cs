﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "DmiIconData")]
public class DmiIconData : ScriptableObject
{

	private static Dictionary<string, DmiIcon> data = new Dictionary<string, DmiIcon>();

	public Dictionary<string, DmiIcon> Data
	{
		get { return data; }
	}

	//Stuff for SpriteManager compatibility
	public Sprite[] getSprites(string path)
	{
//		var iconPath = "icons/" + path + ".dmi";
		if (data.ContainsKey(path))
		{		
			var sprites = data[path].spriteSheet;
			if (sprites != null)
			{
				return sprites;
			}
		}
		Debug.LogError("could not find sprites for key " + path);
		return new Sprite[0];
	}

	private void OnEnable()
	{
		Debug.LogWarning("DmiIconData: OnEnable!");
	IconList<DmiIcon> ilist = DeserializeJson();
		foreach (var icon in ilist.icons)
		{
			var substring = icon.icon.Substring(0, icon.icon.IndexOf(".dmi", StringComparison.Ordinal));
			Sprite[] sprites = Resources.LoadAll<Sprite>(
				substring
			); //todo: consider excluding extensions on java side to avoid substr mess?
			
			icon.spriteSheet = sprites;
			data.Add(substring.Substring("icons/".Length), icon);
		}
	}

//	private void OnDestroy()
//	{
//		Debug.LogWarning("OnDestroy!");
//	}
//
//	private void OnDisable()
//	{
//		Debug.LogWarning("OnDisable!");
//	}
//
//	public void Awake() {
//		Debug.LogWarning("DmiIconData: Awake!");
//	
//	}
	
	private static IconList<DmiIcon> DeserializeJson()
	{
		string myJson = null;
		var asset = Resources.Load(Path.Combine("metadata", "dmi")) as TextAsset;
		if (asset != null)
		{
			//workaround for headerless JSONs
			myJson = "{ \"icons\": " + asset.text + "}";
		} else Debug.LogError("Make sure dmi.json is in Resources/metadata/ !");
			
		var icons = new IconList<DmiIcon>();
		JsonUtility.FromJsonOverwrite(myJson, icons);
		return icons;

	}
	[Serializable]
	private class IconList<T>
	{
		public List<T> icons;
	}
}
