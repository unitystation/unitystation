using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using Newtonsoft.Json;
using Chemistry.Components;

public class GeneratePlantSOs : EditorWindow
{
	/*
	private static Dictionary<string, string> dictonaryErrors;

	[MenuItem("Tools/GeneratePlantSOs")]
	public static void Generate()
	{
		float progressbarStep;
		float progressbarState;
		DirectoryInfo d = new DirectoryInfo(Application.dataPath + @"\Textures\objects\hydroponics\growing");
		FileInfo[] Files = d.GetFiles("*.png");// \\Getting Text files
		var ListFiles = new List<string>();
		dictonaryErrors = new Dictionary<string, string>();
		var PlantDictionary = new Dictionary<string, DefaultPlantData>();
		var PlantDictionaryObject = new Dictionary<string, System.Object>();

		foreach (FileInfo file in Files)
		{
			ListFiles.Add(file.Name);
		}

		var food = (Resources.Load(@"Prefabs\Items\Botany\food") as GameObject);
		var json = (Resources.Load(@"Metadata\plants") as TextAsset).ToString();
		var plats = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

		progressbarStep = 1f / (plats.Count * ListFiles.Count);
		progressbarState = 0;
		foreach (var plat in plats)
		{
			EditorUtility.DisplayProgressBar("Step 1/3 Setting Sprites", "Loading plant: " + plat["name"], progressbarState);
			//\\foreach (var Datapiece in plat)
			//\\{
			//\\	Logger.Log(Datapiece.Key);
			//\\}

			var plantdata = PlantData.CreateNewPlant((PlantData)null);
			plantdata.ProduceObject = food;
			plantdata.Name = plat["name"] as string;

			if (plat.ContainsKey("plantname"))
			{
				plantdata.Plantname = plat["plantname"] as string;
			}
			plantdata.Description = plat["Description"] as string;
			var species = "";
			if (plat.ContainsKey("species"))
			{
				species = (plat["species"] as string);
			}
			else
			{
				Debug.Log($"Unable to find 'species' tag for plant {plantdata.Name}, using 'seed_packet' instead");
				species = (plat["seed_packet"] as string);
				if (species.Contains("seed-"))
				{
					species = species.Replace("seed-", "");
				}
				else if (species.Contains("mycelium-"))
				{
					species = species.Replace("mycelium-", "");
				}
			};

			//plantdata.PacketsSprite = new SpriteSheetAndData();
			//plantdata.PacketsSprite.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\seeds\" + (plat["seed_packet"] as string) + ".png", typeof(Texture2D)) as Texture2D);
			//plantdata.PacketsSprite.setSprites();

			SpriteSheetAndData produceSprite = new SpriteSheetAndData();
			produceSprite.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\harvest\" + species + ".png", typeof(Texture2D)) as Texture2D);
			if (produceSprite.Texture == null)
			{
				produceSprite = new SpriteSheetAndData();
				produceSprite.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\harvest\" + species + "pile.png", typeof(Texture2D)) as Texture2D);
			}
			if (produceSprite.Texture == null)
			{
				produceSprite = new SpriteSheetAndData();
				produceSprite.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\harvest\" + species + "_leaves.png", typeof(Texture2D)) as Texture2D);
			}
			if (produceSprite.Texture == null)
			{
				produceSprite = new SpriteSheetAndData();
				produceSprite.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\harvest\" + species + "pod.png", typeof(Texture2D)) as Texture2D);
			}
			if (produceSprite.Texture == null)
			{
				produceSprite = new SpriteSheetAndData();
				produceSprite.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\harvest\" + species + "s.png", typeof(Texture2D)) as Texture2D);
			}
			if (produceSprite.Texture == null)
			{
				produceSprite = new SpriteSheetAndData();
				produceSprite.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\harvest\" + species + "pepper.png", typeof(Texture2D)) as Texture2D);
			}
			produceSprite.setSprites();



			var dead_sprite = (plat.ContainsKey("dead_Sprite")) ? (plat["dead_Sprite"] as string) : species + "-dead";

			plantdata.DeadSprite = new SpriteSheetAndData();
			plantdata.DeadSprite.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\growing\" + dead_sprite + ".png", typeof(Texture2D)) as Texture2D);
			plantdata.DeadSprite.setSprites();

			plantdata.GrowthSprites = new List<SpriteSheetAndData>();
			foreach (var ListFile in ListFiles)
			{

				if (ListFile.Contains(species))
				{
					var Namecheck = ListFile;
					/*Namecheck = Namecheck.Replace("growing_flowers_", "");
					Namecheck = Namecheck.Replace("growing_fruits_", "");
					Namecheck = Namecheck.Replace("growing_mushrooms_", "");
					Namecheck = Namecheck.Replace("growing_vegetables_", "");
					Namecheck = Namecheck.Replace("growing_", "");
					Namecheck = Namecheck.Split('-')[0];

					if (Namecheck == species)
					{
						EditorUtility.DisplayProgressBar("Step 1/3 Setting Sprites", $"Loading sprite '{ListFile}' for plant {plantdata.Name}", progressbarState);
						if (!ListFile.Contains("-dead"))
						{
							if (!ListFile.Contains("-harvest"))
							{
								//\Assets\Resources\textures\objects\hydroponics\growing\growing_ambrosia_gaia-grow6.png
								var _SpriteSheetAndData = new SpriteSheetAndData();
								_SpriteSheetAndData.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\growing\" + ListFile, typeof(Texture2D)) as Texture2D);
								_SpriteSheetAndData.setSprites();
								plantdata.GrowthSprites.Add(_SpriteSheetAndData);

								//If not found do at end
							}
							else
							{
								var _SpriteSheetAndData = new SpriteSheetAndData();
								_SpriteSheetAndData.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\growing\" + ListFile, typeof(Texture2D)) as Texture2D);
								_SpriteSheetAndData.setSprites();
								plantdata.FullyGrownSprite = _SpriteSheetAndData;
							}

						}

					}

				}
				if (plantdata.FullyGrownSprite == null)
				{
					if (plantdata.GrowthSprites.Count > 0)
					{
						//This seems to be normal
						plantdata.FullyGrownSprite = plantdata.GrowthSprites[plantdata.GrowthSprites.Count - 1];
					}
				}

				progressbarState += progressbarStep;
			}
			//check if sprites are missing
			//if (plantdata.PacketsSprite.Texture == null) { AppendError(plantdata.Name, $"Unable to find seed packet sprite for plant {plantdata.Name}"); }
			//if (plantdata.ProduceSprite.Texture == null) {  }
			if (plantdata.DeadSprite.Texture == null) { AppendError(plantdata.Name, $"Unable to find dead sprite"); }
			if (plantdata.GrowthSprites.Count == 0) { AppendError(plantdata.Name, $"Unable to find growth sprites for plant {plantdata.Name}"); }
			if (plantdata.FullyGrownSprite == null) { AppendError(plantdata.Name, $"Unable to find fully grown sprite"); }



			plantdata.WeedResistance = int.Parse(plat["weed_resistance"].ToString());
			plantdata.WeedGrowthRate = int.Parse(plat["weed_growth_rate"].ToString());
			plantdata.Potency = int.Parse(plat["potency"].ToString());
			plantdata.Endurance = int.Parse(plat["endurance"].ToString());
			plantdata.Yield = int.Parse(plat["plant_yield"].ToString());
			plantdata.Lifespan = int.Parse(plat["lifespan"].ToString());
			plantdata.GrowthSpeed = int.Parse(plat["production"].ToString());

			if (plat.ContainsKey("genes"))
			{
				var genes = JsonConvert.DeserializeObject<List<string>>(plat["genes"].ToString());

				foreach (var gene in genes)
				{
					if (gene == "Perennial_Growth")
					{
						plantdata.PlantTrays.Add(PlantTrays.Perennial_Growth);
					}
					else if (gene == "Fungal Vitality")
					{
						plantdata.PlantTrays.Add(PlantTrays.Fungal_Vitality);
					}
					else if (gene == "Liquid Contents")
					{
						plantdata.PlantTrays.Add(PlantTrays.Liquid_Content);
					}
					else if (gene == "Slippery Skin")
					{
						plantdata.PlantTrays.Add(PlantTrays.Slippery_Skin);
					}
					else if (gene == "Bluespace Activity")
					{
						plantdata.PlantTrays.Add(PlantTrays.Bluespace_Activity);
					}
					else if (gene == "Densified Chemicals")
					{
						plantdata.PlantTrays.Add(PlantTrays.Densified_Chemicals);
					}
					else if (gene == "Capacitive Cell Production")
					{
						plantdata.PlantTrays.Add(PlantTrays.Capacitive_Cell_Production);
					}
					else if (gene == "Weed Adaptation")
					{
						plantdata.PlantTrays.Add(PlantTrays.Weed_Adaptation);
					}
					else if (gene == "Hypodermic Prickles")
					{
						plantdata.PlantTrays.Add(PlantTrays.Hypodermic_Needles);
					}
					else if (gene == "Shadow Emission")
					{
						plantdata.PlantTrays.Add(PlantTrays.Shadow_Emission);
					}
					else if (gene == "Red Electrical Glow")
					{
						plantdata.PlantTrays.Add(PlantTrays.Red_Electrical_Glow);
					}
					else if (gene == "Electrical Activity")
					{
						plantdata.PlantTrays.Add(PlantTrays.Electrical_Activity);
					}
					else if (gene == "Strong Bioluminescence")
					{
						plantdata.PlantTrays.Add(PlantTrays.Strong_Bioluminescence);
					}
					else if (gene == "Bioluminescence")
					{
						plantdata.PlantTrays.Add(PlantTrays.Bioluminescence);
					}
					else if (gene == "Separated Chemicals")
					{
						plantdata.PlantTrays.Add(PlantTrays.Separated_Chemicals);
					}
				}
			}





			//Creating/updating food prefabs
			if (plat.ContainsKey("produce_name"))
			{
				//load existing prefab variant if possible
				GameObject prefabVariant = (GameObject)AssetDatabase.LoadAssetAtPath(@"Assets/Resources/Prefabs/Items/Botany/" + plantdata.Name + ".prefab", typeof(GameObject));

				if (prefabVariant == null)
				{
					GameObject originalPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(@"Assets/Resources/Prefabs/Items/Botany/food.prefab", typeof(GameObject));
					prefabVariant = PrefabUtility.InstantiatePrefab(originalPrefab) as GameObject;
				}
				else
				{
					prefabVariant = PrefabUtility.InstantiatePrefab(prefabVariant) as GameObject;
				}



				var itemAttr = prefabVariant.GetComponent<ItemAttributesV2>();

				//Commented since this are normally private
				//itemAttr.initialName = plat["produce_name"] as string;
				//itemAttr.initialDescription = plat["description"] as string;
				//itemAttr.itemSprites = (new ItemsSprites() { InventoryIcon = produceSprite });

				//add sprite to food
				var spriteRenderer = prefabVariant.GetComponentInChildren<SpriteRenderer>();
				//FFGD  spriteRenderer.sprite = SpriteFunctions.SetupSingleSprite(produceSprite).ReturnFirstSprite();

				var newFood = prefabVariant.GetComponent<GrownFood>();

				//Set plant data for food
				newFood.plantData = plantdata;

				var newReagents = prefabVariant.GetComponent<ReagentContainer>();

				//add reagents to food
				if (plat.ContainsKey("reagents_add"))
				{
					var Chemicals = JsonConvert.DeserializeObject<Dictionary<string, float>>(plat["reagents_add"].ToString());

					var reagents = new List<string>();
					var amounts = new List<float>();
					foreach (var Chemical in Chemicals)
					{
						//ChemicalDictionary[Chemical.Key] = (((int)(Chemical.Value * 100)) * (plantdata.Potency / 100f));
						reagents.Add(Chemical.Key);
						amounts.Add(((int)(Chemical.Value * 100)) * (plantdata.Potency / 100f));
					}

					//newReagents.Reagents = reagents;
					//newReagents.Amounts = amounts;
				}

				plantdata.ProduceObject = PrefabUtility.SaveAsPrefabAsset(prefabVariant, @"Assets/Resources/Prefabs/Items/Botany/" + plantdata.Name + ".prefab");
			}
			else
			{
				plantdata.ProduceObject = null;
			}

			var DefaultPlantData = ScriptableObject.CreateInstance<DefaultPlantData>();
			DefaultPlantData.plantData = plantdata;
			//\\ Creates the folder path


			//\\ Creates the file in the folder path
			Logger.Log(plantdata.Name + " < PlantDictionary");
			PlantDictionary[plantdata.Name] = DefaultPlantData;

			if (plat.ContainsKey("mutates_into"))
			{
				PlantDictionaryObject[plantdata.Name] = plat["mutates_into"];
			}


			//\\Logger.Log(plantdata.GrowthSprites.Count.ToString());

		}




		progressbarStep = 1f / PlantDictionary.Count;
		progressbarState = 0;
		var mutationNameList = new List<string>();
		foreach (var pant in PlantDictionary)
		{
			EditorUtility.DisplayProgressBar("Step 2/3 Setting Mutations", "Loading mutations for: " + pant.Value.plantData.Name, progressbarState += progressbarStep);
			if (PlantDictionaryObject.ContainsKey(pant.Value.plantData.Name))
			{
				var Mutations = JsonConvert.DeserializeObject<List<string>>(PlantDictionaryObject[pant.Value.plantData.Name].ToString());
				foreach (var Mutation in Mutations)
				{
					if(!mutationNameList.Contains(Mutation)){ mutationNameList.Add(Mutation); }
					if (Mutation.Length != 0)
					{
						if (PlantDictionary.ContainsKey(Mutation))
						{
							MutationComparison(pant.Value, PlantDictionary[Mutation]);
							pant.Value.plantData.MutatesInTo.Add((DefaultPlantData)AssetDatabase.LoadAssetAtPath(@"Assets\Resources\ScriptableObjects\Plant default\" + PlantDictionary[Mutation].plantData.Name + ".asset", typeof(DefaultPlantData)));





							if (PlantDictionary[Mutation].plantData.DeadSprite?.Texture == null)
							{

								if (pant.Value.plantData.DeadSprite?.Texture != null)
								{
									PlantDictionary[Mutation].plantData.DeadSprite = new SpriteSheetAndData();
									PlantDictionary[Mutation].plantData.DeadSprite.Texture = pant.Value.plantData.DeadSprite.Texture;
									PlantDictionary[Mutation].plantData.DeadSprite.setSprites();
								}

							}

							if (PlantDictionary[Mutation].plantData.GrowthSprites.Count == 0)
							{
								PlantDictionary[Mutation].plantData.GrowthSprites = pant.Value.plantData.GrowthSprites;
							}
						}
					}
				}
			}
		}
		progressbarStep = 1f / PlantDictionary.Count;
		progressbarState = 0;
		foreach (var pant in PlantDictionary)
		{



			DefaultPlantData defaultPlant = AssetDatabase.LoadMainAssetAtPath(@"Assets\Resources\ScriptableObjects\Plant default\" + pant.Value.plantData.Name + ".asset") as DefaultPlantData;
			if (defaultPlant != null)
			{
				EditorUtility.DisplayProgressBar("Step 3/3 Saving ScriptObjects", "Updating asset: " + pant.Value.plantData.Name, progressbarState += progressbarStep);
				EditorUtility.CopySerialized(pant.Value, defaultPlant);
				AssetDatabase.SaveAssets();
			}
			else
			{
				EditorUtility.DisplayProgressBar("Step 3/3 Saving ScriptObjects", "Creating asset: " + pant.Value.plantData.Name, progressbarState += progressbarStep);
				defaultPlant = ScriptableObject.CreateInstance<DefaultPlantData>();
				EditorUtility.CopySerialized(pant.Value, defaultPlant);
				AssetDatabase.CreateAsset(pant.Value, @"Assets\Resources\ScriptableObjects\Plant default\" + pant.Value.plantData.Name + ".asset");
			}

			if (dictonaryErrors.ContainsKey(pant.Value.plantData.Name))
			{

				if(mutationNameList.Contains(pant.Value.plantData.Name))
				{
					dictonaryErrors[pant.Value.plantData.Name] = $"Mutation {pant.Value.plantData.Name} has some missing sprites\n{dictonaryErrors[pant.Value.plantData.Name]}";
					Debug.LogWarning(dictonaryErrors[pant.Value.plantData.Name]);
				}
				else
				{
					dictonaryErrors[pant.Value.plantData.Name] = $"Plant {pant.Value.plantData.Name} has some missing sprites\n{dictonaryErrors[pant.Value.plantData.Name]}";
					Debug.LogError(dictonaryErrors[pant.Value.plantData.Name]);
				}
			}
		}


		EditorUtility.ClearProgressBar();
		EditorUtility.DisplayDialog("Complete", "Generating default plant ScriptObjects complete", "Close");

	}

	public static void MutationComparison(DefaultPlantData f_rom, DefaultPlantData to)
	{
		if (f_rom.plantData.WeedResistance != to.plantData.WeedResistance)
		{
			to.WeedResistanceChange = to.plantData.WeedResistance = f_rom.plantData.WeedResistance;

		}
		if (f_rom.plantData.WeedGrowthRate != to.plantData.WeedGrowthRate)
		{
			to.WeedGrowthRateChange = to.plantData.WeedGrowthRate = f_rom.plantData.WeedGrowthRate;
		}
		if (f_rom.plantData.GrowthSpeed != to.plantData.GrowthSpeed)
		{
			to.GrowthSpeedChange = to.plantData.GrowthSpeed = f_rom.plantData.GrowthSpeed;
		}
		if (f_rom.plantData.Potency != to.plantData.Potency)
		{
			to.PotencyChange = to.plantData.Potency = f_rom.plantData.Potency;
		}
		if (f_rom.plantData.Endurance != to.plantData.Endurance)
		{
			to.EnduranceChange = to.plantData.Endurance = f_rom.plantData.Endurance;
		}
		if (f_rom.plantData.Yield != to.plantData.Yield)
		{
			to.YieldChange = to.plantData.Yield = f_rom.plantData.Yield;
		}
		if (f_rom.plantData.Lifespan != to.plantData.Lifespan)
		{
			to.LifespanChange = to.plantData.Lifespan = f_rom.plantData.Lifespan;
		}

		if (f_rom.plantData.Lifespan != to.plantData.Lifespan)
		{
			to.LifespanChange = to.plantData.Lifespan = f_rom.plantData.Lifespan;
		}
		if (f_rom.plantData.PlantTrays != to.plantData.PlantTrays)
		{
			foreach (var PlantTray in f_rom.plantData.PlantTrays)
			{
				if (!to.plantData.PlantTrays.Contains(PlantTray))
				{
					to.RemovePlantTrays.Add(PlantTray);
				}
			}
			foreach (var PlantTray in to.plantData.PlantTrays)
			{
				if (!f_rom.plantData.PlantTrays.Contains(PlantTray))
				{
					to.PlantTrays.Add(PlantTray);
				}
			}
		}

		if (f_rom.plantData.ReagentProduction != to.plantData.ReagentProduction)
		{
			foreach (var Stringint in f_rom.plantData.ReagentProduction)
			{
				if (!to.plantData.ReagentProduction.Contains(Stringint))
				{

					to.RemoveReagentProduction.Add(Stringint);
				}
			}
			foreach (var Stringint in to.plantData.ReagentProduction)
			{
				if (!f_rom.plantData.ReagentProduction.Contains(Stringint))
				{
					to.ReagentProduction.Add(Stringint);
				}
			}
		}
	}

	public static void AppendError(string key,string error)
	{
		if (dictonaryErrors.ContainsKey(key))
		{
			dictonaryErrors[key] = $"{dictonaryErrors[key]}\n{error}";
		}
		else
			dictonaryErrors.Add(key, error);
	}
	*/
}