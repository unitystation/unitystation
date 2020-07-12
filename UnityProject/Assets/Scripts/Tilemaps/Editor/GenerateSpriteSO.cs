using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection;

public class GenerateSpriteSO : EditorWindow
{

	public static List<string> ToDel = new List<string>();
	public static Dictionary<string,SpriteDataSO> ToSeve = new Dictionary<string, SpriteDataSO>();

	public static SpriteCatalogue spriteCatalogue;
	[MenuItem("Tools/GenerateSpriteSO")]
	public static void Generate()
	{
		spriteCatalogue = AssetDatabase.LoadAssetAtPath<SpriteCatalogue>(
			"Assets/Resources/ScriptableObjects/SOs singletons/SpriteCatalogueSingleton.asset");
		DirSearch_ex3(Application.dataPath+"/textures"); //
		AssetDatabase.StartAssetEditing();
		foreach (var oDe in ToDel)
		{
			AssetDatabase.DeleteAsset(oDe);
		}

		foreach (var Seve in ToSeve)
		{
			AssetDatabase.CreateAsset(Seve.Value, Seve.Key);
			spriteCatalogue.Catalogue.Add(Seve.Value);
		}
		AssetDatabase.StopAssetEditing();
	}

	public static void DirSearch_ex3(string sDir)
	{
		//Console.WriteLine("DirSearch..(" + sDir + ")");

			//Logger.Log(sDir);

			var Files = Directory.GetFiles(sDir);
			foreach (string f in Files)
			{
				if (f.Contains(".png") && f.Contains(".meta") == false)
				{
					var path =  f;
					var TT = path.Replace(Application.dataPath,"Assets");
					var Sprites = AssetDatabase.LoadAllAssetsAtPath(TT).OfType<Sprite>().ToArray();
					if (Sprites.Length > 1)
					{
						Sprites = Sprites.OrderBy(x => int.Parse(x.name.Substring(x.name.LastIndexOf('_') + 1))).ToArray();
					}
					//yeah If you named your sub sprites rip, have to find another way of ordering them correctly since the editor doesnt want to do that		E
					var EquippedData = (TextAsset)AssetDatabase.LoadAssetAtPath(path.Replace(".png", ".json").Replace(Application.dataPath,"Assets"), typeof(TextAsset));
					var SpriteData = ScriptableObject.CreateInstance<SpriteDataSO>();


					//SpriteData.
					SpriteData = FilloutData(EquippedData,Sprites,  SpriteData);
					ToSeve[f.Replace(".png", ".asset").Replace(Application.dataPath, "Assets")] = SpriteData;
					ToDel.Add(path.Replace(".png", ".json").Replace(Application.dataPath,"Assets"));

					//Gizmos.DrawIcon();
					//DrawIcon(SpriteData,  Sprites[0].texture);
					//https://forum.unity.com/threads/editor-changing-an-items-icon-in-the-project-window.272061/

				}
				//Logger.Log(f);
			}

			foreach (string d in Directory.GetDirectories(sDir))
			{
				DirSearch_ex3(d);
			}

	}

	public static SpriteDataSO FilloutData(TextAsset EquippedData, Sprite[]  Sprites, SpriteDataSO SpriteData)
	{
		SpriteJson spriteJson = null;

		if (EquippedData != null) {
			spriteJson = JsonConvert.DeserializeObject<SpriteJson>(EquippedData.text);
		}
		else
		{
			if (Sprites.Length > 1)
			{
				Logger.LogError("OH NO json File wasn't found for " + Sprites[0].name, Category.Editor);
			}
			SpriteData.Variance.Add(new SpriteDataSO.Variant());
			SpriteData.Variance[0].Frames.Add(new SpriteDataSO.Frame());
			SpriteData.Variance[0].Frames[0].sprite = Sprites[0];
			return SpriteData;
		}
		int variance = 0;
		int frame = 0;
		for (int J = 0; J < spriteJson.Number_Of_Variants; J++)
		{
			SpriteData.Variance.Add(new SpriteDataSO.Variant());
		}

		foreach (var SP in Sprites)
		{
			var info = new SpriteDataSO.Frame();
			info.sprite = SP;
			if (spriteJson.Delays.Count > 0)
			{
				info.secondDelay = spriteJson.Delays[variance][frame];
			}

			SpriteData.Variance[variance].Frames.Add(info);
			if (variance >= (spriteJson.Number_Of_Variants - 1))
			{
				variance = 0;
				frame++;
			}
			else
			{
				variance++;
			}
		}
		return SpriteData;

	}
}
