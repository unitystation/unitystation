using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[System.Serializable]
public class PlantData
{
	private static readonly System.Random random = new System.Random();

	public GameObject ProduceObject;
	public string Name;
	public string Plantname;

	public string Description;

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
	public int Potency = 15;
	public int Endurance = 15;
	public int Yield = 40;
	public int Lifespan = 25;
	public List<PlantTrays> PlantTrays = new List<PlantTrays>();
	public List<Reagent> ReagentProduction = new List<Reagent>();
	//public List<DefaultPlantData> MutatesInTo = new List<DefaultPlantData>();

	public List<GameObject> MutatesInToGameObject = new List<GameObject>();

	public PlantEvolveStats plantEvolveStats = new PlantEvolveStats();

	public int Age;
	public int NextGrowthStageProgress;
	public float Health;

	private const int MAX_POTENCY = 100;

	//Use static methods to create new instances of PlantData
	private PlantData()
	{
	}

	/// <summary>
	/// Gets a new instance of PlantData based on param
	/// </summary>
	/// <param name="defaultPlantData">DefaultPlantData to copy</param>
	/// <returns></returns>
	public static PlantData CreateNewPlant(DefaultPlantData defaultPlantData)
	{
		PlantData newPlant = new PlantData();
		newPlant.SetValues(defaultPlantData);
		newPlant.Health = 100;
		newPlant.Age = 0;
		return newPlant;
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
	/// <param name="_PlantData.plantEvolveStats"></param>
	public void MutateTo(PlantData _PlantData)
	{
		if (_PlantData.plantEvolveStats == null) return;
		Plantname = _PlantData.Plantname;
		Description = _PlantData.Description;
		Name = _PlantData.Name;
		ProduceObject = _PlantData.ProduceObject;


		GrowthSpritesSOs =  _PlantData.GrowthSpritesSOs;

		FullyGrownSpriteSO = _PlantData.FullyGrownSpriteSO;
		DeadSpriteSO = _PlantData.DeadSpriteSO;
		MutatesInToGameObject = _PlantData.MutatesInToGameObject;

		WeedResistance = WeedResistance + _PlantData.plantEvolveStats.WeedResistanceChange;
		WeedGrowthRate = WeedGrowthRate + _PlantData.plantEvolveStats.WeedGrowthRateChange;
		GrowthSpeed = GrowthSpeed + _PlantData.plantEvolveStats.GrowthSpeedChange;
		Potency = Mathf.Clamp(Potency + _PlantData.plantEvolveStats.PotencyChange, 0, MAX_POTENCY);
		Endurance = Endurance + _PlantData.plantEvolveStats.EnduranceChange;
		Yield = Yield + _PlantData.plantEvolveStats.YieldChange;
		Lifespan = Lifespan + _PlantData.plantEvolveStats.LifespanChange;


		PlantTrays = (_PlantData.plantEvolveStats.PlantTrays.Union(PlantTrays)).ToList();
		CombineReagentProduction(_PlantData.plantEvolveStats.ReagentProduction);
	}

	/// <summary>
	/// Initializes plant with data another plant object
	/// </summary>
	/// <param name="_PlantData">data to copy</param>
	private void SetValues(PlantData _PlantData)
	{
		Plantname = _PlantData.Plantname;
		Description = _PlantData.Description;
		Name = _PlantData.Name;
		ProduceObject = _PlantData.ProduceObject;
		GrowthSpritesSOs = _PlantData.GrowthSpritesSOs;
		FullyGrownSpriteSO = _PlantData.FullyGrownSpriteSO;
		DeadSpriteSO = _PlantData.DeadSpriteSO;
		WeedResistance = _PlantData.WeedResistance;
		WeedGrowthRate = _PlantData.WeedGrowthRate;
		GrowthSpeed = _PlantData.GrowthSpeed;
		Potency = _PlantData.Potency;
		Endurance = _PlantData.Endurance;
		Yield = _PlantData.Yield;
		Lifespan = _PlantData.Lifespan;
		PlantTrays = _PlantData.PlantTrays;
		ReagentProduction = _PlantData.ReagentProduction;
		MutatesInToGameObject = _PlantData.MutatesInToGameObject;
	}

	/// <summary>
	/// Initializes plant with data from default plant
	/// </summary>
	/// <param name="DefaultPlantData">DefaultPlantData.plantdata's values are copied</param>
	private void SetValues(DefaultPlantData DefaultPlantData)
	{
		var _PlantData = DefaultPlantData.plantData;
		Name = _PlantData.Name;
		Plantname = _PlantData.Plantname;
		Description = _PlantData.Description;
		if (ProduceObject == null)
		{
			ProduceObject = _PlantData.ProduceObject;
		}

		if (GrowthSpritesSOs.Count == 0)
		{
			GrowthSpritesSOs = _PlantData.GrowthSpritesSOs;
		}

		if (FullyGrownSpriteSO == null)
		{
			FullyGrownSpriteSO = _PlantData.FullyGrownSpriteSO;
		}

		if (DeadSpriteSO == null)
		{
			DeadSpriteSO = _PlantData.DeadSpriteSO;
		}

		WeedResistance = _PlantData.WeedResistance;
		WeedGrowthRate = _PlantData.WeedGrowthRate;
		GrowthSpeed = _PlantData.GrowthSpeed;
		Potency = _PlantData.Potency;
		Endurance = _PlantData.Endurance;
		Yield = _PlantData.Yield;
		Lifespan = _PlantData.Lifespan;


		PlantTrays = (_PlantData.PlantTrays.Union(PlantTrays)).ToList();
		MutatesInToGameObject = (_PlantData.MutatesInToGameObject.Union(MutatesInToGameObject)).ToList();
		CombineReagentProduction(_PlantData.ReagentProduction);
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
					if (_Reagent.Name == Reagent.Name)
					{
						if (_Reagent.Ammount > Reagent.Ammount)
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
		MutateTo(newPlantData.GetComponent<SeedPacket>().plantData);
		//UpdatePlant(oldPlantData.Name, Name);
	}

	/// <summary>
	/// Triggers stat mutation and chance to mutate into new plant
	/// </summary>
	/// <param name="modification"></param>
	public void NaturalMutation(PlantTrayModification modification)
	{
		//Chance to actually mutate
		//if (random.Next(1, 2) != 1) return;

		//Stat mutations
		WeedResistance = StatMutation(WeedResistance, 100);
		WeedGrowthRate = BadStatMutation(WeedGrowthRate, 100);
		GrowthSpeed = StatMutation(GrowthSpeed, 100);
		Potency = StatMutation(Potency, 100);
		Endurance = StatMutation(Endurance, 100);
		Yield = StatMutation(Yield, 100);
		Lifespan = StatMutation(Lifespan, 100);
		switch (modification)
		{
			case PlantTrayModification.None:
				break;
			case PlantTrayModification.WeedResistance:
				WeedResistance = SpecialStatMutation(WeedResistance, 100);
				break;
			case PlantTrayModification.WeedGrowthRate:
				WeedGrowthRate = SpecialStatMutation(WeedGrowthRate, 100);
				break;
			case PlantTrayModification.GrowthSpeed:
				GrowthSpeed = SpecialStatMutation(GrowthSpeed, 100);
				break;
			case PlantTrayModification.Potency:
				Potency = SpecialStatMutation(Potency, 100);
				break;
			case PlantTrayModification.Endurance:
				Endurance = SpecialStatMutation(Endurance, 100);
				break;
			case PlantTrayModification.Yield:
				Yield = SpecialStatMutation(Yield, 100);
				break;
			case PlantTrayModification.Lifespan:
				Lifespan = SpecialStatMutation(Lifespan, 100);
				break;
		}

		CheckMutation(WeedResistance, 0, 100);
		CheckMutation(WeedGrowthRate, 0, 100);
		CheckMutation(GrowthSpeed, 0, 100);
		CheckMutation(Potency, 0, 100);
		CheckMutation(Endurance, 0, 100);
		CheckMutation(Yield, 0, 100);
		CheckMutation(Lifespan, 0, 100);
		if (random.Next(100) > 95)
		{
			Mutation();
		}
	}

	private int CheckMutation(int num, int min, int max)
	{
		if (num < min)
		{
			return (min);
		}

		else if (num > max)
		{
			return (max);
		}

		return (num);
	}

	private static int BadStatMutation(float stat, float maxStat)
	{
		return ((int) stat + random.Next(-(int) Math.Ceiling((maxStat / 100f) * 5),
			(int) Math.Ceiling((maxStat / 100f) * 2)));
	}

	private static int SpecialStatMutation(float stat, float maxStat)
	{
		return ((int) stat + random.Next(0, (int) Math.Ceiling((maxStat / 100f) * 7)));
	}

	private static int StatMutation(float stat, float maxStat)
	{
		return ((int) stat + random.Next(-(int) Math.Ceiling((maxStat / 100f) * 2),
			(int) Math.Ceiling((maxStat / 100f) * 5)));
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

		public void copyFrom(DefaultPlantData defaultPlantData)
		{
			WeedResistanceChange = defaultPlantData.WeedResistanceChange;
			WeedGrowthRateChange = defaultPlantData.WeedGrowthRateChange;
			GrowthSpeedChange = defaultPlantData.GrowthSpeedChange;
			PotencyChange = defaultPlantData.PotencyChange;;
			EnduranceChange= defaultPlantData.EnduranceChange;
			YieldChange= defaultPlantData.YieldChange;
			LifespanChange= defaultPlantData.LifespanChange;
			PlantTrays = defaultPlantData.PlantTrays;
			ReagentProduction = defaultPlantData.ReagentProduction;
			RemovePlantTrays = defaultPlantData.RemovePlantTrays;
			RemoveReagentProduction = defaultPlantData.RemoveReagentProduction;
		}
	}
}

/// <summary>
/// Holds Reagent information
/// </summary>
[System.Serializable]
public class Reagent
{
	public string Name;
	public int Ammount;
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