using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

using System.IO;
using System;


[CreateAssetMenu(fileName = "PlayerCustomisationData", menuName = "ScriptableObjects/PlayerCustomisation", order = 1)]
public class PlayerCustomisationData : ScriptableObject
{
	public SpriteSheetAndData Equipped;
	public string Name;
	public PlayerCustomisation Type;

	public Gender gender = Gender.Neuter;

	public PlayerCustomisationData This;

	private static bool AlreadyLoaded;
	private static Lobby.LobbyManager CC;

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
		//Logger.Log("PPP " + this.name);
		InitializePool();
	}


	public void InitializePool()
	{
		if (CC == null){ 
			CC = UnityEngine.Object.FindObjectOfType<Lobby.LobbyManager>();
		}

		if (ClothFactoryReference == null)
		{
			ClothFactoryReference = UnityEngine.Object.FindObjectOfType<ClothFactory>();
		}


		if (CC != null) {
			if (!CC.characterCustomization.playerCustomisationData.ContainsKey(Type)) {
				CC.characterCustomization.playerCustomisationData[Type] = new Dictionary<string, PlayerCustomisationData>();
			}
			CC.characterCustomization.playerCustomisationData[Type][Name] = this;
		}

		if (ClothFactoryReference != null)
		{
			if (!ClothFactoryReference.playerCustomisationData.ContainsKey(Type))
			{
				ClothFactoryReference.playerCustomisationData[Type] = new Dictionary<string, PlayerCustomisationData>();
			}
			ClothFactoryReference.playerCustomisationData[Type][Name] = this;		
		}

	}

	//public void OnValidate()
	//{
	//	if (Equipped.Texture != null)
	//	{
	//		if (this.name != Equipped.Texture.name)
	//		{
	//			Name = Equipped.Texture.name.Substring(12);
	//			Type = PlayerCustomisation.FacialHair;
	//			string assetPath = AssetDatabase.GetAssetPath(this.GetInstanceID());
	//			AssetDatabase.RenameAsset(assetPath, Equipped.Texture.name);
	//			AssetDatabase.SaveAssets();
	//		}

	//	}
	//}

	public static void getPlayerCustomisationDatas(List<PlayerCustomisationData>  DataPCD)
	{
		DataPCD.Clear();
		var PCD = Resources.LoadAll<PlayerCustomisationData>("textures/clothing/undergarments");
		foreach (var PCDObj in PCD)
		{
			DataPCD.Add(PCDObj);
		}

		PCD = Resources.LoadAll<PlayerCustomisationData>("textures/mobs/races/_shared textures/customisation");
		foreach (var PCDObj in PCD)
		{
			DataPCD.Add(PCDObj);
		}

		//string[] dirs = Directory.GetDirectories(Application.dataPath, "textures/clothing/undergarments", SearchOption.AllDirectories); //could be changed later not to load everything to save start-up times 
		//foreach (string dir in dirs)
		//{
		//	//Should yield For a frame to Increase performance
		//	loadFolder(dir, DataPCD);
		//	foreach (string subdir in Directory.GetDirectories(dir, "*", SearchOption.AllDirectories))
		//	{
		//		loadFolder(subdir, DataPCD);
		//	}
		//}

		//dirs = Directory.GetDirectories(Application.dataPath, "textures/mobs/races/_shared textures/customisation", SearchOption.AllDirectories); //could be changed later not to load everything to save start-up times 
		//foreach (string dir in dirs)
		//{
		//	//Should yield For a frame to Increase performance
		//	loadFolder(dir, DataPCD);
		//	foreach (string subdir in Directory.GetDirectories(dir, "*", SearchOption.AllDirectories))
		//	{
		//		loadFolder(subdir, DataPCD);
		//	}
		//}
	}

	private static void loadFolder(string folderpath, List<PlayerCustomisationData> DataPCD)
	{
		folderpath = folderpath.Substring(folderpath.IndexOf("Resources", StringComparison.Ordinal) + "Resources".Length);
		foreach (var PCDObj in Resources.LoadAll<PlayerCustomisationData>(folderpath))
		{
			if (!DataPCD.Contains(PCDObj))
			{
				DataPCD.Add(PCDObj);
			}
		}
	}
}


public enum PlayerCustomisation{
	Null = 0,
	FacialHair = 1,
	HairStyle = 2,
	Underwear = 3,
	Undershirt= 4,
	Socks = 5,
	BodySprites = 6,
	//Others as needed, 
}