using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using System;

[CreateAssetMenu(fileName = "PlayerTextureData", menuName = "ScriptableObjects/PlayerTextureData", order = 1)]
public class PlayerTextureData : ScriptableObject
{
	public RaceVariantTextureData Base;
	public RaceVariantTextureData Male;
	public RaceVariantTextureData Female;
	public List<RaceVariantTextureData> Other;

	private static ClothFactory ClothFactoryReference;

	public void Awake()
	{
		InitializePool();
	}

	private void OnEnable()
	{

		SceneManager.sceneLoaded -= OnSceneLoaded;
		SceneManager.sceneLoaded += OnSceneLoaded;
	}
	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{

		InitializePool();
	}


	public void InitializePool()
	{
		if (ClothFactoryReference == null)
		{
			ClothFactoryReference = UnityEngine.Object.FindObjectOfType<ClothFactory>();
		}

		if (ClothFactoryReference != null)
		{
			if (ClothFactoryReference.RaceData.ContainsKey(this.name))
			{
				Logger.LogError("a PlayerTextureData Has the same name as another one name " + this.name + " Please rename one of them to a different name");
			}
			ClothFactoryReference.RaceData[this.name] = this;
		}
	}

	public static void getClothingDatas(List<PlayerTextureData> DataPCD)
	{
		DataPCD.Clear();
		var PCD = Resources.LoadAll<PlayerTextureData>("textures/mobs/races");
		foreach (var PCDObj in PCD)
		{
			DataPCD.Add(PCDObj);
		}
	}

}

[System.Serializable]
public class RaceVariantTextureData
{
	public SpriteSheetAndData Head;
	public SpriteSheetAndData Eyes;
	public SpriteSheetAndData Torso;
	public SpriteSheetAndData ArmRight;
	public SpriteSheetAndData ArmLeft;
	public SpriteSheetAndData HandRight;
	public SpriteSheetAndData HandLeft;
	public SpriteSheetAndData LegRight;
	public SpriteSheetAndData LegLeft;
}


public enum BodyPartSpriteName
{
	Null,
	Head,
	Eyes,
	Torso,
	ArmRight, ArmLeft,
	HandRight, HandLeft,
	LegRight, LegLeft,
}