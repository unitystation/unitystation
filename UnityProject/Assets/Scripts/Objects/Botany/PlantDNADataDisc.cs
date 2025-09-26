using System;
using Systems.Botany;
using UnityEngine;

public class PlantDNADataDisc : MonoBehaviour
{
	public PlantData LoadedData;


	public void Awake()
	{
		LoadedData.Endurance = -1;
		LoadedData.WeedResistance = -1;
		LoadedData.WeedGrowthRate = -1;
		LoadedData.GrowthSpeed = -1;
		LoadedData.Potency = -1;
		LoadedData.Yield = -1;
		LoadedData.Lifespan = -1;
		LoadedData.PlantTrays.Clear();
		LoadedData.ReagentProduction.Clear();
	}

	public bool IsEmpty()
	{
		if (LoadedData.Endurance == -1
		    && LoadedData.WeedResistance == -1
			&& LoadedData.WeedGrowthRate == -1
			&& LoadedData.GrowthSpeed == -1
			&& LoadedData.Potency == -1
			&& LoadedData.Yield == -1
			&& LoadedData.Lifespan == -1
			&& LoadedData.PlantTrays.Count == 0
			&& LoadedData.ReagentProduction.Count == 0)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
}
