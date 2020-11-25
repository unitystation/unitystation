using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public class AddressablePicker : EditorWindow
{
	private static bool refresh = false;

	private static string[] Options;

	public static string[] options
	{
		get
		{
			if (catalogueData == null || Options == null)
			{
				catalogueData = GetCatalogueData();
				Options = catalogueData.SoundAndMusic.ToArray();
			}
			return Options;
		}
	}
	public static CatalogueData catalogueData = null;

	public static CatalogueData GetCatalogueData()
	{
		return  AssetDatabase.LoadAssetAtPath<CatalogueData>("Assets/CachedData/CatalogueData.asset");
	}

	public static string GetCataloguePath()
	{
		var path = Application.dataPath.Remove(Application.dataPath.IndexOf("/Assets"));
		path = path + "/AddressablePackingProjects/SoundAndMusic/ServerData/"; //Make OS agnostic
		Logger.Log(path);
		var Files = System.IO.Directory.GetFiles(path);
		string FoundFile = "";
		foreach (var File in Files)
		{
			//Logger.Log(File);
			if (File.EndsWith(".json"))
			{
				if (FoundFile != "")
				{
					Logger.LogError("two catalogues present please only ensure one");
				}
				FoundFile = File;
			}
		}

		if (FoundFile == "")
		{
			Logger.LogWarning("missing json file");
			return "";
		}

		return FoundFile;
	}

	public static void Refresh()
	{
		var FoundFile = GetCataloguePath();
		JObject o1 = JObject.Parse(File.ReadAllText((@FoundFile.Replace("/", @"\"))));
		var IDs = o1.GetValue("m_InternalIds");
		var ListIDs = IDs.ToObject<List<string>>().Where(x => x.Contains(".bundle") == false);
		Options = ListIDs.ToArray();
		catalogueData = AssetDatabase.LoadAssetAtPath<CatalogueData>("Assets/CachedData/CatalogueData.asset");
		catalogueData.SoundAndMusic = ListIDs.ToList();
		EditorUtility.SetDirty(catalogueData);
	}

}
