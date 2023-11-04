using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Objects.Botany;
using Items.Botany;
using Logs;

namespace Systems.Botany
{
	[Serializable]
	public class PlantData
	{
		private static readonly System.Random random = new System.Random();

		[Tooltip("What the growing plant produces when harvested.")]
		public GameObject ProduceObject;

		[Tooltip("Name of the growing plant, NOT the produce itself.")]
		public string PlantName;

		//public SpriteSheetAndData ProduceSprite;
		//public List<SpriteSheetAndData> GrowthSprites = new List<SpriteSheetAndData>();
		//public SpriteSheetAndData FullyGrownSprite;
		//public SpriteSheetAndData DeadSprite;

		public SpriteDataSO DeadSpriteSO = null;
		public SpriteDataSO FullyGrownSpriteSO = null;
		public List<SpriteDataSO> GrowthSpritesSOs = new List<SpriteDataSO>();


		//add Fully grown sprite with Animations
		public int WeedResistance = 50; //Dank
		public int WeedGrowthRate = 10;
		public int GrowthSpeed = 60;
		public int Potency = 10;
		public int Endurance = 15;
		[Tooltip("Determines how much produce is harvested from a plant. Final amount of produce harvested is the yield divided by 10 rounded to the nearest integer.")]
		public int Yield = 30;
		[Tooltip("Determines how long the plant lives for.")]
		public int Lifespan = 25;
		public List<PlantTrays> PlantTrays = new List<PlantTrays>();
		public List<Reagent> ReagentProduction = new List<Reagent>();

		public List<GameObject> MutatesInToGameObject = new List<GameObject>();

		public PlantEvolveStats plantEvolveStats = new PlantEvolveStats();

		public int Age;
		public int NextGrowthStageProgress;
		public float Health;

		private const int MAX_STAT = 100;
		private const int MIN_STAT = 0;

		//Use static methods to create new instances of PlantData
		private PlantData()
		{
		}

		/// <summary>
		/// Gets a new instance of PlantData based on param
		/// </summary>
		/// <param name="plantData">PlantData to copy</param>
		/// <returns></returns>
		public static PlantData CreateNewPlant(PlantData plantData)
		{
			PlantData newPlant = new PlantData();
			newPlant.SetValues(plantData);
			newPlant.Health = 100;
			newPlant.Age = 0;
			return newPlant;
		}

		public static PlantData MutateNewPlant(PlantData plantData, PlantTrayModification modification)
		{
			PlantData newPlant = new PlantData();
			newPlant.SetValues(plantData);
			newPlant.Health = 100;
			newPlant.Age = 0;
			newPlant.NaturalMutation(modification);
			return newPlant;
		}

		/// <summary>
		/// Mutates the plant instance this is run against
		/// </summary>
		/// <param name="plantData"></param>
		public void MutateTo(PlantData plantData)
		{
			if (plantData.plantEvolveStats == null) return;
			PlantName = plantData.PlantName;
			ProduceObject = plantData.ProduceObject;


			GrowthSpritesSOs = plantData.GrowthSpritesSOs;

			FullyGrownSpriteSO = plantData.FullyGrownSpriteSO;
			DeadSpriteSO = plantData.DeadSpriteSO;
			MutatesInToGameObject = plantData.MutatesInToGameObject;

			var evolveStats = plantData.plantEvolveStats;
			WeedResistance = AddToStat(WeedResistance, evolveStats.WeedResistanceChange);
			WeedGrowthRate = AddToStat(WeedGrowthRate, evolveStats.WeedGrowthRateChange);
			GrowthSpeed = AddToStat(GrowthSpeed, evolveStats.GrowthSpeedChange);
			Potency = AddToStat(Potency, evolveStats.PotencyChange);
			Endurance = AddToStat(Endurance, evolveStats.EnduranceChange);
			Yield = AddToStat(Yield, evolveStats.YieldChange);
			Lifespan = AddToStat(Lifespan, evolveStats.LifespanChange);

			PlantTrays = (evolveStats.PlantTrays.Union(PlantTrays)).ToList();
			CombineReagentProduction(evolveStats.ReagentProduction);
		}

		/// <summary>
		/// Initializes plant with data another plant object
		/// </summary>
		/// <param name="plantData">data to copy</param>
		private void SetValues(PlantData plantData)
		{
			PlantName = plantData.PlantName;
			ProduceObject = plantData.ProduceObject;
			GrowthSpritesSOs = plantData.GrowthSpritesSOs;
			FullyGrownSpriteSO = plantData.FullyGrownSpriteSO;
			DeadSpriteSO = plantData.DeadSpriteSO;
			WeedResistance = plantData.WeedResistance;
			WeedGrowthRate = plantData.WeedGrowthRate;
			GrowthSpeed = plantData.GrowthSpeed;
			Potency = plantData.Potency;
			Endurance = plantData.Endurance;
			Yield = plantData.Yield;
			Lifespan = plantData.Lifespan;
			PlantTrays = plantData.PlantTrays;
			ReagentProduction = plantData.ReagentProduction;
			MutatesInToGameObject = plantData.MutatesInToGameObject;
		}

		/// <summary>
		/// Combine plants reagents removing any duplicates, Keeps highest yield
		/// </summary>
		/// <param name="Reagents">New reagents to combine</param>
		private void CombineReagentProduction(List<Reagent> Reagents)
		{
			var ToRemove = new List<Reagent>();
			Reagents.AddRange(ReagentProduction);
			foreach (var Reagent in Reagents)
			{
				foreach (var _Reagent in Reagents)
				{
					if (_Reagent != Reagent)
					{
						if (_Reagent.ChemistryReagent == Reagent.ChemistryReagent)
						{
							if (_Reagent.Amount > Reagent.Amount)
							{
								ToRemove.Add(Reagent);
							}
							else
							{
								ToRemove.Add(_Reagent);
							}
						}
					}
				}
			}

			foreach (var Reagent in ToRemove)
			{
				Reagents.Remove(Reagent);
			}

			ReagentProduction = Reagents;
		}

		/// <summary>
		/// Triggers plant in tray to mutate if possible
		/// Loads a random mutation out of the plants MutatesInTo list
		/// </summary>
		public void Mutation()
		{
			if (MutatesInToGameObject.Count == 0) return;


			var newPlantData = MutatesInToGameObject.PickRandom();
			if (newPlantData == null)
			{
				return;
			}

			//var oldPlantData = plantData;
			if (newPlantData.TryGetComponent<SeedPacket>(out var packet))
			{
				MutateTo(packet.plantData);
			}
			else
			{
				Loggy.LogError($"{nameof(SeedPacket)} component missing on {newPlantData}! Cannot mutate.", Category.Botany);
			}

			//UpdatePlant(oldPlantData.Name, Name);
		}

		/// <summary>
		/// Triggers stat mutation and chance to mutate into new plant
		/// </summary>
		/// <param name="modification"></param>
		public void NaturalMutation(PlantTrayModification modification)
		{
			//Stat mutations
			WeedResistance = StatMutation(WeedResistance, StatMutationType.Normal);
			WeedGrowthRate = StatMutation(WeedGrowthRate, StatMutationType.Bad);
			GrowthSpeed = StatMutation(GrowthSpeed, StatMutationType.Normal);
			Potency = StatMutation(Potency, StatMutationType.Normal);
			Endurance = StatMutation(Endurance, StatMutationType.Normal);
			Yield = StatMutation(Yield, StatMutationType.Normal);
			Lifespan = StatMutation(Lifespan, StatMutationType.Normal);
			switch (modification)
			{
				case PlantTrayModification.None:
					break;
				case PlantTrayModification.WeedResistance:
					WeedResistance = StatMutation(WeedResistance, StatMutationType.Special);
					break;
				case PlantTrayModification.WeedGrowthRate:
					WeedGrowthRate = StatMutation(WeedGrowthRate, StatMutationType.Special);
					break;
				case PlantTrayModification.GrowthSpeed:
					GrowthSpeed = StatMutation(GrowthSpeed, StatMutationType.Special);
					break;
				case PlantTrayModification.Potency:
					Potency = StatMutation(Potency, StatMutationType.Special);
					break;
				case PlantTrayModification.Endurance:
					Endurance = StatMutation(Endurance, StatMutationType.Special);
					break;
				case PlantTrayModification.Yield:
					Yield = StatMutation(Yield, StatMutationType.Special);
					break;
				case PlantTrayModification.Lifespan:
					Lifespan = StatMutation(Lifespan, StatMutationType.Special);
					break;
			}

			if (random.Next(100) > 95)
			{
				Mutation();
			}
		}

		private enum StatMutationType
		{
			Normal,
			Special,
			Bad
		}

		private int StatMutation(int stat, StatMutationType statMutationType)
		{
			var addition = 0;
			if (statMutationType == StatMutationType.Normal)
			{
				addition = random.Next(2, 5);
			}
			else if (statMutationType == StatMutationType.Special)
			{
				addition = random.Next(0, 7);
			}
			else if (statMutationType == StatMutationType.Bad)
			{
				addition = random.Next(-5, 2);
			}
			return AddToStat(stat, addition);
		}

		private int AddToStat(int stat, int addition)
		{
			stat += addition;
			return Mathf.Clamp(stat, MIN_STAT, MAX_STAT);
		}

		[System.Serializable]
		public class PlantEvolveStats
		{
			public int WeedResistanceChange; //Dank
			public int WeedGrowthRateChange;
			public int GrowthSpeedChange;
			public int PotencyChange;
			public int EnduranceChange;
			public int YieldChange;
			public int LifespanChange;

			public List<PlantTrays> PlantTrays = new List<PlantTrays>();
			public List<Reagent> ReagentProduction = new List<Reagent>();

			public List<PlantTrays> RemovePlantTrays = new List<PlantTrays>();
			public List<Reagent> RemoveReagentProduction = new List<Reagent>();

		}
	}

	/// <summary>
	/// Holds Reagent information
	/// </summary>
	[System.Serializable]
	public class Reagent
	{
		public Chemistry.Reagent ChemistryReagent;
		public int Amount;
	}


	public enum PlantTrays
	{
		Perennial_Growth,
		Weed_Adaptation,
		Fungal_Vitality,
		Hypodermic_Needles,
		Liquid_Content,
		Shadow_Emission,
		Slippery_Skin,
		Bluespace_Activity,
		Densified_Chemicals,
		Capacitive_Cell_Production,
		Red_Electrical_Glow,
		Electrical_Activity,
		Strong_Bioluminescence,
		Bioluminescence,
		Separated_Chemicals,
	}
}
