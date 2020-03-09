using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class PlantData
{
	public GameObject ProduceObject;
	public string Name;
	public string Plantname;
	public string Description;
	public SpriteSheetAndData PacketsSprite;
	public SpriteSheetAndData ProduceSprite;
	public List<SpriteSheetAndData> GrowthSprites = new List<SpriteSheetAndData>();
	public SpriteSheetAndData FullyGrownSprite;
	public SpriteSheetAndData DeadSprite;
	//add Fully grown sprite with Animations
	public int WeedResistance = 50;// -1; //Dank
	public int WeedGrowthRate = 10;//-1;
	public int GrowthSpeed = 60;//-1;
	public int Potency = 15;//-1;
	public int Endurance = 15;//-1;
	public int Yield = 40;//-1;
	public int Lifespan = 25;//-1;
	public List<PlantTrays> PlantTrays = new List<PlantTrays>();
	public List<Reagent> ReagentProduction = new List<Reagent>();
	public List<DefaultPlantData> MutatesInTo = new List<DefaultPlantData>();

	/// <summary>
	/// Mutates the plant instance this is run against
	/// </summary>
	/// <param name="_DefaultPlantData"></param>
	public void MutateTo(DefaultPlantData _DefaultPlantData)
	{
		if (_DefaultPlantData == null) return;
		Plantname = _DefaultPlantData.plantData.Plantname;
		Description = _DefaultPlantData.plantData.Description;
		Name = _DefaultPlantData.plantData.Name;
		ProduceObject = _DefaultPlantData.plantData.ProduceObject;
		PacketsSprite = _DefaultPlantData.plantData.PacketsSprite;
		ProduceSprite = _DefaultPlantData.plantData.ProduceSprite;
		GrowthSprites = _DefaultPlantData.plantData.GrowthSprites;
		FullyGrownSprite = _DefaultPlantData.plantData.FullyGrownSprite;
		DeadSprite = _DefaultPlantData.plantData.DeadSprite;

		MutatesInTo = _DefaultPlantData.plantData.MutatesInTo;

		WeedResistance = WeedResistance + _DefaultPlantData.WeedResistanceChange;
		WeedGrowthRate = WeedGrowthRate + _DefaultPlantData.WeedGrowthRateChange;
		GrowthSpeed = GrowthSpeed + _DefaultPlantData.GrowthSpeedChange;
		Potency = Potency + _DefaultPlantData.PotencyChange;
		Endurance = Endurance + _DefaultPlantData.EnduranceChange;
		Yield = Yield + _DefaultPlantData.YieldChange;
		Lifespan = Lifespan + _DefaultPlantData.LifespanChange;


		PlantTrays = (_DefaultPlantData.PlantTrays.Union(PlantTrays)).ToList();
		CombineReagentProduction(_DefaultPlantData.ReagentProduction);
	}

	/// <summary>
	/// Initializes plant with data another plant object
	/// </summary>
	/// <param name="_PlantData">data to copy</param>
	public void SetValues(PlantData _PlantData)
	{
		Plantname = _PlantData.Plantname;
		Description = _PlantData.Description;
		Name = _PlantData.Name;
		ProduceObject = _PlantData.ProduceObject;
		PacketsSprite = _PlantData.PacketsSprite;
		ProduceSprite = _PlantData.ProduceSprite;
		GrowthSprites = _PlantData.GrowthSprites;
		FullyGrownSprite = _PlantData.FullyGrownSprite;
		DeadSprite = _PlantData.DeadSprite;
		WeedResistance = _PlantData.WeedResistance;
		WeedGrowthRate = _PlantData.WeedGrowthRate;
		GrowthSpeed = _PlantData.GrowthSpeed;
		Potency = _PlantData.Potency;
		Endurance = _PlantData.Endurance;
		Yield = _PlantData.Yield;
		Lifespan = _PlantData.Lifespan;
		PlantTrays = _PlantData.PlantTrays;
		ReagentProduction = _PlantData.ReagentProduction;
		MutatesInTo = _PlantData.MutatesInTo;
	}

	/// <summary>
	/// Initializes plant with data from default plant
	/// </summary>
	/// <param name="DefaultPlantData">DefaultPlantData.plantdata's values are copied</param>
	public void SetValues(DefaultPlantData DefaultPlantData)
	{
		var _PlantData = DefaultPlantData.plantData;
		Name = _PlantData.Name;
		Plantname = _PlantData.Plantname;
		Description = _PlantData.Description;
		if (ProduceObject == null)
		{
			ProduceObject = _PlantData.ProduceObject;
		}
		if (PacketsSprite?.Texture == null)
		{
			PacketsSprite = _PlantData.PacketsSprite;
		}
		if (ProduceSprite?.Texture == null)
		{
			ProduceSprite = _PlantData.ProduceSprite;
		}

		if (GrowthSprites.Count == 0)
		{
			GrowthSprites = _PlantData.GrowthSprites;
		}

		if (FullyGrownSprite?.Texture == null)
		{
			FullyGrownSprite = _PlantData.FullyGrownSprite;
		}
		if (DeadSprite?.Texture == null)
		{
			DeadSprite = _PlantData.DeadSprite;
		}
		WeedResistance = _PlantData.WeedResistance;
		WeedGrowthRate = _PlantData.WeedGrowthRate;
		GrowthSpeed = _PlantData.GrowthSpeed;
		Potency = _PlantData.Potency;
		Endurance = _PlantData.Endurance;
		Yield = _PlantData.Yield;
		Lifespan = _PlantData.Lifespan;



		PlantTrays = (_PlantData.PlantTrays.Union(PlantTrays)).ToList();
		MutatesInTo = (_PlantData.MutatesInTo.Union(MutatesInTo)).ToList();
		CombineReagentProduction(_PlantData.ReagentProduction);
	}

	/// <summary>
	/// Combine plants reagents removing any duplicates, Keeps highest yield
	/// </summary>
	/// <param name="Reagents">New reagents to combine</param>
	public void CombineReagentProduction(List<Reagent> Reagents)
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
						else {
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
