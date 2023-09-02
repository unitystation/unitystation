using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Logs;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public class AddressablePicker : EditorWindow
{
	private static Dictionary<string, string[]> Options;

	public static Dictionary<string, string[]> options
	{
		get
		{
			if (catalogueData == null || Options.Count == 0)
			{
				catalogueData = GetCatalogueData();
				Options = new Dictionary<string, string[]>();
				foreach (var keyv in catalogueData.Data)
				{
					Options[keyv.Key] = keyv.Value.ToArray();
				}

				if (Options.Count == 0)
				{
					Refresh();
				}
			}

			return Options;
		}
	}

	public static CatalogueData catalogueData = null;

	public static CatalogueData GetCatalogueData()
	{
		return AssetDatabase.LoadAssetAtPath<CatalogueData>("Assets/CachedData/CatalogueData.asset");
	}

	public static List<string> GetCataloguePath()
	{
		var path = Application.dataPath.Remove(Application.dataPath.IndexOf("/Assets"));
		path += "/AddressablePackingProjects";
		Loggy.Log(path, Category.Addressables);
		var Directories = Directory.GetDirectories(path);
		var FoundFiles = new List<string>();
		foreach (var Directori in Directories)
		{
			var newpath = Directori + "/ServerData";
			if (Directory.Exists(newpath))
			{
				var Files = Directory.GetFiles(newpath);

				string FoundFile = "";
				foreach (var File in Files)
				{
					//Logger.Log(File);
					if (File.EndsWith(".json"))
					{
						if (FoundFile != "")
						{
							Loggy.LogError("two catalogues present please only ensure one", Category.Addressables);
						}

						FoundFile = File;
					}
				}

				if (FoundFile == "")
				{
					Loggy.LogWarning("missing json file", Category.Addressables);
				}
				else
				{
					FoundFiles.Add(FoundFile);
				}
			}
		}

		return FoundFiles;
	}

	public static void Refresh()
	{
		var FoundFiles = GetCataloguePath();
		foreach (var FoundFile in FoundFiles)
		{
			JObject o1 = JObject.Parse(File.ReadAllText((@FoundFile.Replace("/", @"/"))));
			var IDs = o1.GetValue("m_InternalIds");
			var ListIDs = IDs.ToObject<List<string>>().Where(x => x.Contains(".bundle") == false);

			catalogueData = AssetDatabase.LoadAssetAtPath<CatalogueData>("Assets/CachedData/CatalogueData.asset");
			var flip = new FileInfo(FoundFile);
			var ToPutInList = ListIDs.ToList();
			ToPutInList .Insert(0, "null");
			catalogueData.Data[flip.Directory.Parent.Name] = ToPutInList;

		}

		Options = new Dictionary<string, string[]>();
		foreach (var keyv in catalogueData.Data)
		{
			Options[keyv.Key] = keyv.Value.ToArray();
		}

		EditorUtility.SetDirty(catalogueData);
    }
}
