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
	public static Dictionary<string, SpriteDataSO> ToSeve = new Dictionary<string, SpriteDataSO>();

	public static SpriteCatalogue spriteCatalogue;

	[MenuItem("Tools/StopAssetEditing")]
	public static void StopAssetEditing()
	{
		AssetDatabase.StopAssetEditing();
		return;
	}


	[MenuItem("Tools/Convert Json Sprites")]
	public static void ConvertJsonSprites()
	{
		spriteCatalogue = AssetDatabase.LoadAssetAtPath<SpriteCatalogue>("Assets/Resources/ScriptableObjects/SOs singletons/SpriteCatalogueSingleton.asset");
		ToSeve.Clear();
		ToDel.Clear();
		DirSearch_ex3(Application.dataPath + "/SpriteJsonToSO");

		foreach (var oDe in ToDel)
		{
			AssetDatabase.DeleteAsset(oDe);
		}

		foreach (var Seve in ToSeve)
		{
			AssetDatabase.CreateAsset(Seve.Value, Seve.Key);
			Seve.Value.Awake();
		}
		ToSeve.Clear();
		ToDel.Clear();
		AssetDatabase.SaveAssets();
	}


	[MenuItem("Tools/GenerateSpriteSO")]
	public static void Generate()
	{
		//AssetDatabase.StopAssetEditing();
		//spriteCatalogue = AssetDatabase.LoadAssetAtPath<SpriteCatalogue>(
		//	"Assets/Resources/ScriptableObjects/SOs singletons/SpriteCatalogueSingleton.asset");
		//
		//	DirSearch_ex3Prefab(Application.dataPath + "/Resources/Prefabs/Items"); //
		//
		AssetDatabase.StartAssetEditing();
		DirSearch_ex3(Application.dataPath + "/Textures");
		AssetDatabase.StopAssetEditing();
		AssetDatabase.SaveAssets();
		return;

		// TODO The following code is unreachable! Remove it or make it usable. 😣
		var pathe = Application.dataPath + "/Resources/Prefabs";
		var aDDll = LoadAllPrefabsOfType<SpriteHandler>(pathe);
		foreach (var SH in aDDll)
		{
			//foreach (var Sprite in SH.Sprites)
			//{
				//var SO = PullOutSO(Sprite.Texture);
				//if (SH.SubCatalogue.Contains(SO) == false)
				//{
					//SH.SubCatalogue.Add(SO);
				//}
			//}

			var SR = SH.GetComponent<SpriteRenderer>();
			if (SR != null)
			{
				if (SR.sprite != null)
				{
					//SH.PresentSpriteSet = PullOutSO(SR.sprite.texture);
				}
			}

			try
			{
				PrefabUtility.SavePrefabAsset(GetRoot(SH.gameObject));
			}
			catch
			{
				Logger.Log(GetRoot(SH.gameObject).name + "Not root apparently");
			}
		}



		return;
		AssetDatabase.StartAssetEditing();
		var AAA = FindAssetsByType<SpriteCatalogue>();
		foreach (var a in AAA)
		{
			EditorUtility.SetDirty(a);
		}

		AssetDatabase.StopAssetEditing();
		AssetDatabase.SaveAssets();

		return;
		var DD = LoadAllPrefabsOfType<SeedPacket>(Application.dataPath + "/Resources/Prefabs/Items/Botany");
		//AssetDatabase.StartAssetEditing();
		/*
		var DDD = LoadAllPrefabsOfType<SeedPacket>(Application.dataPath + "/Resources/Prefabs/Items/Botany");
		foreach (var d in DDD)
		{
			if (d == null ) continue;
			if (d.plantData?.DeadSpriteSO == null && d.defaultPlantData != null)
			{

				d.plantData = d.defaultPlantData.plantData;
				d.plantData.DeadSpriteSO = PullOutSO(d.plantData.DeadSprite.Texture);
				d.plantData.FullyGrownSpriteSO = PullOutSO(d.plantData.FullyGrownSprite.Texture);

				d.plantData.GrowthSpritesSOs.Clear();
				foreach (var TT in d.plantData.GrowthSprites)
				{
					d.plantData.GrowthSpritesSOs.Add(PullOutSO(TT.Texture));
				}

				foreach (var Mutates in d.plantData.MutatesInTo)
				{
					if (Mutates.plantData.ProduceObject == null)
					{
						var Seepak = FindSeedPacket(Mutates);
						if (Seepak == null)
						{
							Seepak = GenerateDummySeedPacket(Mutates);
						}

						Mutates.plantData.ProduceObject = GenerateDummyProduce(Mutates.plantData, Seepak);
					}

					var foodit = Mutates.plantData.ProduceObject.GetComponent<GrownFood>();


					if (foodit != null)
					{
						var DSeepak = FindSeedPacket(Mutates);
						if (DSeepak == null)
						{
							DSeepak = GenerateDummySeedPacket(Mutates);
						}

						foodit.seedPacket = DSeepak;
						PrefabUtility.SavePrefabAsset(foodit.gameObject);
					}

					if (foodit != null && d.plantData.MutatesInToGameObject.Contains(foodit.seedPacket) == false)
					{
						d.plantData.MutatesInToGameObject.Add(Mutates.plantData.ProduceObject.GetComponent<GrownFood>().seedPacket);
					}
				}




				PrefabUtility.SavePrefabAsset(d.gameObject);

			}


		}





		foreach (var d in DD)
		{
			if (d.defaultPlantData == null) continue;
			d.plantData.MutatesInToGameObject.Clear();
			d.plantData = d.defaultPlantData.plantData;

			d.plantData.DeadSpriteSO = PullOutSO(d.plantData.DeadSprite.Texture);
			d.plantData.FullyGrownSpriteSO = PullOutSO(d.plantData.FullyGrownSprite.Texture);

			d.plantData.GrowthSpritesSOs.Clear();
			foreach (var TT in d.plantData.GrowthSprites)
			{
				d.plantData.GrowthSpritesSOs.Add(PullOutSO(TT.Texture));
			}


			foreach (var Mutates in d.plantData.MutatesInTo)
			{
				if (Mutates.plantData.ProduceObject == null)
				{
					var Seepak = FindSeedPacket(Mutates);
					if (Seepak == null)
					{
						Seepak = GenerateDummySeedPacket(Mutates);
					}

					Mutates.plantData.ProduceObject = GenerateDummyProduce(Mutates.plantData, Seepak);
				}

				var foodit = Mutates.plantData.ProduceObject.GetComponent<GrownFood>();


				if (foodit != null)
				{
					var DSeepak = FindSeedPacket(Mutates);
					if (DSeepak == null)
					{
						DSeepak = GenerateDummySeedPacket(Mutates);
					}

					foodit.seedPacket = DSeepak;
					PrefabUtility.SavePrefabAsset(foodit.gameObject);
				}

				if (foodit != null && d.plantData.MutatesInToGameObject.Contains(foodit.seedPacket) == false)
				{
					d.plantData.MutatesInToGameObject.Add(Mutates.plantData.ProduceObject.GetComponent<GrownFood>().seedPacket);
				}
			}

			PrefabUtility.SavePrefabAsset(d.gameObject);
		}

		//AssetDatabase.StopAssetEditing();
		return;


		foreach (var oDe in ToDel)
		{
			AssetDatabase.DeleteAsset(oDe);
		}

		foreach (var Seve in ToSeve)
		{
			AssetDatabase.CreateAsset(Seve.Value, Seve.Key);
			spriteCatalogue.Catalogue.Add(Seve.Value);
		}
		*/
	}

	public static GameObject FindSeedPacket(DefaultPlantData defaultPlantData)
	{
		var DD = LoadAllPrefabsOfType<SeedPacket>(Application.dataPath + "/Resources/Prefabs/Items/Botany");
		foreach (var D in DD)
		{
			//if (D.defaultPlantData == defaultPlantData)
			//{
			//return D.gameObject;
			//}
		}

		return null;
	}


	public static GameObject FindProduce(SeedPacket seedPacket)
	{
		var DD = LoadAllPrefabsOfType<GrownFood>(Application.dataPath + "/Resources/Prefabs/Items/Botany");
		foreach (var D in DD)
		{
			if (D.seedPacket.GetComponent<SeedPacket>() == seedPacket)
			{
				return D.gameObject;
			}
		}

		return null;
	}


	public static GameObject GenerateDummySeedPacket(DefaultPlantData plantData)
	{
		AssetDatabase.CopyAsset("Assets/Resources/Prefabs/Items/Botany/Seeds/AutoGenerated/seed packet Variant.prefab",
			"Assets/Resources/Prefabs/Items/Botany/Seeds/AutoGenerated/" + plantData.plantData.Name + ".prefab");
		var gameObject =
			AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Items/Botany/Seeds/AutoGenerated/" +
			                                          plantData.plantData.Name + ".prefab");
		var DDA = gameObject.GetComponent<SeedPacket>();

		//DDA.defaultPlantData= plantData;
		PrefabUtility.SavePrefabAsset(gameObject);
		return gameObject;
	}

	public static GameObject GenerateDummyProduce(PlantData plantData, GameObject Seepdpakes)
	{
		AssetDatabase.CopyAsset("Assets/Resources/Prefabs/Items/Botany/Produce/AutoGenerated/base.prefab",
			"Assets/Resources/Prefabs/Items/Botany/Produce/AutoGenerated/" + plantData.Name + ".prefab");
		var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(
			"Assets/Resources/Prefabs/Items/Botany/Produce/AutoGenerated/" +
			plantData.Name + ".prefab");
		gameObject.GetComponent<GrownFood>().seedPacket = Seepdpakes;
		PrefabUtility.SavePrefabAsset(gameObject);
		return gameObject;
		//seedPacket
	}


	public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
	{
		List<T> assets = new List<T>();
		string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));
		for (int i = 0; i < guids.Length; i++)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
			T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
			if (asset != null)
			{
				assets.Add(asset);
			}
		}

		return assets;
	}


	/*
	  	var COols = FindAssetsByType<ClothingData>();
		//AssetDatabase.StartAssetEditing();
		foreach (var COol in COols)
		{
			PullOutEquippedData(COol.Base);
			PullOutEquippedData(COol.Base_Adjusted);
			//PullOutEquippedData(COol.DressVariant);
			//foreach (var ariant in COol.Variants)
			//{
				//PullOutEquippedData(ariant);
			//}

			EditorUtility.SetDirty(COol);

		}
		var COolsBeltData = FindAssetsByType<BeltData>();
		//AssetDatabase.StartAssetEditing();
		foreach (var COol in COolsBeltData)
		{
			PullOutEquippedData(COol.sprites);
			EditorUtility.SetDirty(COol);

		}

		var HeadsetDatas = FindAssetsByType<HeadsetData>();

		foreach (var COol in HeadsetDatas)
		{
			PullOutEquippedData(COol.Sprites);
			EditorUtility.SetDirty(COol);

		}

		var  ContainerDatas= FindAssetsByType<ContainerData>();
		foreach (var COol in ContainerDatas)
		{
			PullOutEquippedData(COol.Sprites);
			EditorUtility.SetDirty(COol);
		}
	 */

	public static EquippedData PullOutEquippedData(EquippedData ToProcess)
	{
		//ToProcess.SpriteEquipped = PullOutSO(ToProcess.Equipped.Texture);
		//ToProcess.SpriteItemIcon = PullOutSO(ToProcess.ItemIcon.Texture);
		//ToProcess.SpriteInHandsLeft = PullOutSO(ToProcess.InHandsLeft.Texture);
		//ToProcess.SpriteInHandsRight = PullOutSO(ToProcess.InHandsRight.Texture);
		return ToProcess;
	}

	public static SpriteDataSO PullOutSO(Texture2D In2D)
	{
		var path = AssetDatabase.GetAssetPath(In2D);
		return AssetDatabase.LoadAssetAtPath<SpriteDataSO>(path.Replace(".png", ".asset"));
	}


	public static GameObject GetRoot(GameObject gameObject)
	{
		if (gameObject.transform.parent != null)
		{
			return GetRoot(gameObject.transform.parent.gameObject);
		}
		else
		{
			return gameObject;
		}
	}

	public static void DirSearch_ex3Prefab(string sDir)
	{
		//Console.WriteLine("DirSearch..(" + sDir + ")");

		//Logger.Log(sDir);
		var aDDll = LoadAllPrefabsOfType<ItemAttributesV2>(sDir);
		foreach (var f in aDDll)
		{
			//f.ItemSprites.SpriteInventoryIcon =
			//	PullOutSO(f.ItemSprites.InventoryIcon.Texture);

			//f.ItemSprites.SpriteLeftHand =
			//	PullOutSO(f.ItemSprites.LeftHand.Texture);

			//f.ItemSprites.SpriteRightHand =
			//	PullOutSO(f.ItemSprites.RightHand.Texture);
			PrefabUtility.SavePrefabAsset(f.gameObject);
			//Logger.Log(f);
		}
	}

	public static List<T> LoadAllPrefabsOfType<T>(string path) where T : MonoBehaviour
	{
		if (path != "")
		{
			if (path.EndsWith("/"))
			{
				path = path.TrimEnd('/');
			}
		}

		DirectoryInfo dirInfo = new DirectoryInfo(path);
		FileInfo[] fileInf = dirInfo.GetFiles("*.prefab", SearchOption.AllDirectories);

		//loop through directory loading the game object and checking if it has the component you want
		List<T> prefabComponents = new List<T>();
		foreach (FileInfo fileInfo in fileInf)
		{
			string fullPath = fileInfo.FullName.Replace(@"\", "/");
			string assetPath = "Assets" + fullPath.Replace(Application.dataPath, "");
			GameObject prefab = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;

			if (prefab != null)
			{
				T hasT = prefab.GetComponent<T>();
				if (hasT != null)
				{
					prefabComponents.Add(hasT);
				}

				var hasTT = prefab.GetComponentsInChildren<T>();

				foreach (var S in hasTT)
				{


					prefabComponents.Add(S);
				}
			}
		}

		return prefabComponents;
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
				//if (Files.Contains(f.Replace(".json", ".png")) == false) return;

				var path = f;
				var TT = path.Replace(Application.dataPath, "Assets");
				var Sprites = AssetDatabase.LoadAllAssetsAtPath(TT).OfType<Sprite>().ToArray();
				if (Sprites.Length > 1)
				{
					Sprites = Sprites.OrderBy(x => int.Parse(x.name.Substring(x.name.LastIndexOf('_') + 1))).ToArray();
				}

				//yeah If you named your sub sprites rip, have to find another way of ordering them correctly since the editor doesnt want to do that		E
				var EquippedData = (TextAsset) AssetDatabase.LoadAssetAtPath(
					path.Replace(".png", ".json").Replace(Application.dataPath, "Assets"), typeof(TextAsset));
				var SpriteData = ScriptableObject.CreateInstance<SpriteDataSO>();


				//SpriteData.
				SpriteData = FilloutData(EquippedData, Sprites, SpriteData);

				var saev = f.Replace(".png", ".asset").Replace(Application.dataPath, "Assets");
				ToSeve[saev] = SpriteData;
				ToDel.Add(path.Replace(".png", ".json").Replace(Application.dataPath, "Assets"));

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

	public static SpriteDataSO FilloutData(TextAsset EquippedData, Sprite[] Sprites, SpriteDataSO SpriteData)
	{
		SpriteJson spriteJson = null;

		if (EquippedData != null)
		{
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