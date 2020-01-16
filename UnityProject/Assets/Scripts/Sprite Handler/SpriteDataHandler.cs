using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine.Serialization;

/// <summary>
/// Stores and Process SpriteData
/// To be used in SpriteHandler
/// </summary>
public class SpriteDataHandler : MonoBehaviour
{
	[FormerlySerializedAs("SpriteInfos")]
	public SpriteData Infos;


	void Start() {
		AddSprites();
	}

	public void AddSprites() { 
		foreach (var Data in Sprites)
		{
			Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(Data));
		}
	}

	public List<SpriteSheetAndData> Sprites = new List<SpriteSheetAndData>();

	public class SpriteInfo
	{
		public Sprite sprite;
		public float waitTime;
	}
}