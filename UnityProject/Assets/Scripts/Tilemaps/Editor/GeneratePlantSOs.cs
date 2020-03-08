using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using Newtonsoft.Json;

public class GeneratePlantSOs : EditorWindow
{
	[MenuItem("Tools/GeneratePlantSOs")]
	public static void Generate()
	{
		DirectoryInfo d = new DirectoryInfo(Application.dataPath + @"\Textures\objects\hydroponics\growing");
		FileInfo[] Files = d.GetFiles("*.png");// \\Getting Text files
		var ListFiles = new List<string>();
		var PlantDictionary = new Dictionary<string, DefaultPlantData>();
		var PlantDictionaryObject = new Dictionary<string, System.Object>();
		foreach (FileInfo file in Files)
		{
			ListFiles.Add(file.Name);
		}

		var food = (Resources.Load(@"Prefabs\Items\Botany\food") as GameObject);
		var json = (Resources.Load(@"Metadata\plants") as TextAsset).ToString();
		var plats = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
		foreach (var plat in plats)
		{
			//\\foreach (var Datapiece in plat)
			//\\{
			//\\	Logger.Log(Datapiece.Key);
			//\\}

			var plantdata = new PlantData
			{
				ProduceObject = food,
				Name = plat["name"] as string
			};
			if (plat.ContainsKey("plantname"))
			{
				plantdata.Plantname = plat["plantname"] as string;
			}
			plantdata.Description = plat["Description"] as string;
			var seed_packet = "";
			if (plat.ContainsKey("species"))
			{
				seed_packet = (plat["species"] as string);
			}
			else
			{
				seed_packet = (plat["seed_packet"] as string);
				if (seed_packet.Contains("seed-"))
				{
					seed_packet = seed_packet.Replace("seed-", "");
				}
				else if (seed_packet.Contains("mycelium-"))
				{
					seed_packet = seed_packet.Replace("mycelium-", "");
				}
			}

			//
			Logger.Log(seed_packet);
			//Logger.Log(plat["seed_packet"] as string);
			//Logger.Log("harvest_" + seed_packet);

			plantdata.PacketsSprite = new SpriteSheetAndData();
			plantdata.PacketsSprite.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\seeds\seeds_" + (plat["seed_packet"] as string) + ".png", typeof(Texture2D)) as Texture2D);
			plantdata.PacketsSprite.setSprites();

			plantdata.ProduceSprite = new SpriteSheetAndData();
			plantdata.ProduceSprite.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\harvest\harvest_" + seed_packet + ".png", typeof(Texture2D)) as Texture2D);

			//Application.dataPath
			if (plantdata.ProduceSprite.Texture == null)
			{
				plantdata.ProduceSprite.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\harvest\harvest_" + seed_packet + "pile" + ".png", typeof(Texture2D)) as Texture2D);
			}
			if (plantdata.ProduceSprite.Texture == null)
			{
				var EEEseed_packet = (plat["seed_packet"] as string);
				if (EEEseed_packet.Contains("seed-"))
				{
					EEEseed_packet = EEEseed_packet.Replace("seed-", "");
				}
				else if (EEEseed_packet.Contains("mycelium-"))
				{
					EEEseed_packet = EEEseed_packet.Replace("mycelium-", "");
				}
				plantdata.ProduceSprite.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\harvest\harvest_" + EEEseed_packet + ".png", typeof(Texture2D)) as Texture2D);
				if (plantdata.ProduceSprite.Texture == null)
				{
					plantdata.ProduceSprite.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\harvest\harvest_" + EEEseed_packet + "s" + ".png", typeof(Texture2D)) as Texture2D);
				}
				if (plantdata.ProduceSprite.Texture == null)
				{
					plantdata.ProduceSprite.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\harvest\harvest_" + seed_packet + "s" + ".png", typeof(Texture2D)) as Texture2D);
				}
			}
			plantdata.ProduceSprite.setSprites();

			plantdata.GrowthSprites = new List<SpriteSheetAndData>();
			//var Growingsprites = new List<string>();
			foreach (var ListFile in ListFiles)
			{

				if (ListFile.Contains(seed_packet))
				{
					var Namecheck = ListFile;
					Namecheck = Namecheck.Replace("growing_flowers_", "");
					Namecheck = Namecheck.Replace("growing_fruits_", "");
					Namecheck = Namecheck.Replace("growing_mushrooms_", "");
					Namecheck = Namecheck.Replace("growing_vegetables_", "");
					Namecheck = Namecheck.Replace("growing_", "");
					Namecheck = Namecheck.Split('-')[0];

					if (Namecheck == seed_packet)
					{
						if (!ListFile.Contains("-dead"))
						{
							if (!ListFile.Contains("-harvest"))
							{
								//var _ListFile = ListFile.Replace(".png", "");
								//\\Growingsprites.Add(ListFile);
								//\Assets\Resources\textures\objects\hydroponics\growing\growing_ambrosia_gaia-grow6.png
								var _SpriteSheetAndData = new SpriteSheetAndData();
								_SpriteSheetAndData.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\growing\" + ListFile, typeof(Texture2D)) as Texture2D);
								_SpriteSheetAndData.setSprites();
								plantdata.GrowthSprites.Add(_SpriteSheetAndData);

								//\\If not found do at end
							}
							else
							{
								//Logger.Log("got harvest");

								var _SpriteSheetAndData = new SpriteSheetAndData();
								_SpriteSheetAndData.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\growing\" + ListFile, typeof(Texture2D)) as Texture2D);
								_SpriteSheetAndData.setSprites();
								plantdata.FullyGrownSprite = _SpriteSheetAndData;
							}

						}
						else
						{
							//Logger.Log("got DeadSprite");

							//var _ListFile = ListFile.Replace(".png", "");
							var _SpriteSheetAndData = new SpriteSheetAndData();
							_SpriteSheetAndData.Texture = (AssetDatabase.LoadAssetAtPath(@"Assets\textures\objects\hydroponics\growing\" + ListFile, typeof(Texture2D)) as Texture2D);
							_SpriteSheetAndData.setSprites();
							plantdata.DeadSprite = _SpriteSheetAndData;
						}

					}

				}
				if (plantdata.FullyGrownSprite == null)
				{
					if (plantdata.GrowthSprites.Count > 0)
					{
						plantdata.FullyGrownSprite = plantdata.GrowthSprites[plantdata.GrowthSprites.Count - 1];
					}
				}
			}
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
			if (plat.ContainsKey("reagents_add"))
			{
				var Chemicals = JsonConvert.DeserializeObject<Dictionary<string, float>>(plat["reagents_add"].ToString());

				foreach (var Chemical in Chemicals)
				{
					var SInt = new Reagent();
					SInt.Ammount = (int)(Chemical.Value * 100);
					SInt.Name = Chemical.Key;
					plantdata.ReagentProduction.Add(SInt);
				}
			}

			var DefaultPlantData = new DefaultPlantData
			{
				plantData = plantdata
			};
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


		foreach (var pant in PlantDictionary)
		{
			if (PlantDictionaryObject.ContainsKey(pant.Value.plantData.Name))
			{
				var Mutations = JsonConvert.DeserializeObject<List<string>>(PlantDictionaryObject[pant.Value.plantData.Name].ToString());
				foreach (var Mutation in Mutations)
				{
					if (Mutation.Length != 0)
					{
						if (PlantDictionary[Mutation] != null)
						{
							MutationComparison(pant.Value, PlantDictionary[Mutation]);
							pant.Value.plantData.MutatesInTo.Add((DefaultPlantData)AssetDatabase.LoadAssetAtPath(@"Assets\Resources\ScriptableObjects\Plant default\" + PlantDictionary[Mutation].plantData.Name + ".asset", typeof(DefaultPlantData)));
							
						}



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
		foreach (var pant in PlantDictionary)
		{
			AssetDatabase.CreateAsset(pant.Value, @"Assets\Resources\ScriptableObjects\Plant default\" + pant.Value.plantData.Name + ".asset");
		}
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
}