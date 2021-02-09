using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;

public class AddressablesDevBuildSetup : IPreprocessBuild
{
	public int callbackOrder
	{
		get { return 0; }
	}


	public void OnPreprocessBuild(BuildTarget target, string path)
	{


		AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
		AddressableAssetSettings.BuildPlayerContent();

		var Gamedata = AssetDatabase.LoadAssetAtPath<GameObject>(
			"Assets/Resources/Prefabs/SceneConstruction/NestedManagers/GameData.prefab");
		if (Gamedata.GetComponent<GameData>().DevBuild == false)
		{
			return;
		}
		EditorUtility.SetDirty(Gamedata);
		AssetDatabase.SaveAssets();

		var paths = GetCataloguePath();
		foreach (var addpath in paths)
		{

			var flip = new FileInfo(addpath);
			var towork = flip.Directory;

			// var endpath = Application.dataPath.Remove(Application.dataPath.IndexOf("/Assets"));
			// endpath = endpath + towork;
			var DD = towork;
			var newendpath = Application.streamingAssetsPath + "/AddressableCatalogues/" + flip.Directory.Parent.Name + "/";
			var newDD = new DirectoryInfo(newendpath);

			Directory.CreateDirectory(newendpath);
			CopyFilesRecursively(DD, newDD);
			Logger.Log(newendpath);
			if (File.Exists(newendpath +  flip.Directory.Parent.Name +".json"))
			{
				File.Delete(newendpath +  flip.Directory.Parent.Name +".json");
			}

			if (File.Exists(newendpath +  flip.Directory.Parent.Name +".hash"))
			{
				File.Delete(newendpath +  flip.Directory.Parent.Name +".hash");
			}


			var Files = System.IO.Directory.GetFiles(newendpath);
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


			System.IO.File.Move(FoundFile, newendpath + flip.Directory.Parent.Name +".json");
			System.IO.File.Move(FoundFile.Replace(".json", ".hash"), newendpath +flip.Directory.Parent.Name +".hash");
			JObject o1 = JObject.Parse(File.ReadAllText((@newendpath + "/" + flip.Directory.Parent.Name +".json".Replace("/", @"\"))));

			var IDs = (JArray) o1["m_InternalIds"];
			for (int i = 0; i < IDs.Count; i++)
			{
				var newID = IDs[i].ToString();
				newID = newID.Replace("AddressablePackingProjects/" + flip.Directory.Parent.Name + "/ServerData/",
					"unitystation_Data/StreamingAssets/AddressableCatalogues/"+  flip.Directory.Parent.Name + "/");
				//Assets < Editor, build > unitystation_Data
				//Check cache in app data if changes aren't applying
				IDs[i] = newID;
			}

			File.WriteAllText(newendpath + "/" + flip.Directory.Parent.Name + ".json",
				Newtonsoft.Json.JsonConvert.SerializeObject(o1, Newtonsoft.Json.Formatting.None));
		}

		// var ff = new Dictionary<string,string>();
		// var nice = ff["STOP"].Length;
	}

	public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
	{
		foreach (DirectoryInfo dir in source.GetDirectories())
			CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
		foreach (FileInfo file in source.GetFiles())
			file.CopyTo(Path.Combine(target.FullName, file.Name), true);
	}

	public static List<string> GetCataloguePath()
	{
		var path = Application.dataPath.Remove(Application.dataPath.IndexOf("/Assets"));
		//path = path + "/AddressablePackingProjects/SoundAndMusic/ServerData"; //Make OS agnostic
		path = path + "/AddressablePackingProjects";
		Logger.Log(path);
		var Directories = System.IO.Directory.GetDirectories(path);
		var FoundFiles = new List<string>();
		foreach (var Directori in Directories)
		{
			var newpath = Directori + "/ServerData";
			if (System.IO.Directory.Exists(newpath))
			{
				var Files = System.IO.Directory.GetFiles(newpath);

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
				}
				else
				{
					FoundFiles.Add(FoundFile);
				}
			}
		}

		return FoundFiles;
	}


	[PostProcessBuild]
	public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
	{
		Logger.LogWarning("Cleaning Streaming assets of AddressableCatalogues");
		System.IO.DirectoryInfo di = new DirectoryInfo(Application.streamingAssetsPath + "/AddressableCatalogues/");

		foreach (FileInfo file in di.GetFiles("*", SearchOption.AllDirectories))
		{
			//Logger.Log(file.ToString());
			file.Delete();
		}
	}
}