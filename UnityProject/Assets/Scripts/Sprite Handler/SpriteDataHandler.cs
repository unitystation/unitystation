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
	//TODO
	//Maybe a dictionary so you can easily look up in hands and stuff like that
	//With enum
	[FormerlySerializedAs("SpriteInfos")]
	[Tooltip("Do not edit this by hand! Use set up sheet!")]
	public SpriteData Infos;

	// List of sprites to be used. If the sprite has an animation, only the zeroth is needed.
	// Left hand and right hand sprites need to be first in the list.
	public List<Sprite> spriteList = new List<Sprite>();

	private SpriteJson spriteJson;

	public virtual void OnEnable()
	{
	}

	void Start()
	{
		AddSprites();
	}

	public void AddSprites()
	{
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

	public class SpriteJson
	{
		public List<List<float>> Delays;
		public int Number_Of_Variants;
		public int Frames_Of_Animation;
	}
}