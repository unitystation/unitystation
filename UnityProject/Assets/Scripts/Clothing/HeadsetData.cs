using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using System;

[CreateAssetMenu(fileName = "HeadsetData", menuName = "ScriptableObjects/HeadsetData", order = 2)]
public class HeadsetData : BaseClothData
{
	public EquippedData Sprites;
	public HeadsetKyes Key;

	public override Sprite SpawnerIcon()
	{
		return Sprites.ItemIcon.Sprites[0];
	}

	public override void InitializePool()
	{
		if (Spawn.HeadSetStoredData.ContainsKey(this.name) && Spawn.HeadSetStoredData[this.name] != this)
		{
			Logger.LogError("a HeadsetData Has the same name as another one. name " + this.name + ". Please rename one of them to a different name");
		}
		Spawn.HeadSetStoredData[this.name] = this;

	}


	public static void getHeadsetData(List<HeadsetData> DataPCD)
	{
		DataPCD.Clear();
		var PCD = Resources.LoadAll<HeadsetData>("textures/clothing");
		foreach (var PCDObj in PCD)
		{
			DataPCD.Add(PCDObj);
		}

		string[] dirs = Directory.GetDirectories(Application.dataPath, "textures/clothing", SearchOption.AllDirectories); //could be changed later not to load everything to save start-up times


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

	private static void loadFolder(string folderpath, List<HeadsetData> DataPCD)
	{
		folderpath = folderpath.Substring(folderpath.IndexOf("Resources", StringComparison.Ordinal) + "Resources".Length);
		foreach (var PCDObj in Resources.LoadAll<HeadsetData>(folderpath))
		{
			if (!DataPCD.Contains(PCDObj))
			{
				DataPCD.Add(PCDObj);
			}
		}
	}
}


[System.Serializable]
public class HeadsetKyes
{
	public EncryptionKeyType EncryptionKey;
}