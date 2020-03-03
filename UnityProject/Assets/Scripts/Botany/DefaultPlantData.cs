﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "DefaultPlantData", menuName = "ScriptableObjects/DefaultPlantData", order = 1)]
public class DefaultPlantData : ScriptableObject
{

	public static Dictionary<string, DefaultPlantData> PlantDictionary = new Dictionary<string, DefaultPlantData>(); //Temporary until chairbender implements the network animations


	public PlantData plantData;

	public int WeedResistanceChange; //Dank
	public int WeedGrowthRateChange;
	public int GrowthSpeedChange;
	public int PotencyChange;
	public int EnduranceChange;
	public int YieldChange;
	public int LifespanChange;

	public List<PlantTrays> PlantTrays = new List<PlantTrays>();
	public List<StringInt> ReagentProduction = new List<StringInt>();

	public List<PlantTrays> RemovePlantTrays = new List<PlantTrays>();
	public List<StringInt> RemoveReagentProduction = new List<StringInt>();

	public static void getDatas(List<DefaultPlantData> Datas)
	{
		Datas.Clear();
		var Data = Resources.LoadAll<DefaultPlantData>("ScriptableObjects");
		foreach (var DataObj in Data)
		{
			Datas.Add(DataObj);
		}
	}

	public void Awake()
	{	
#if UNITY_EDITOR
		{
			if (DefaultPlantDataSOs.Instance == null)
			{
				Resources.LoadAll<DefaultPlantDataSOs>("ScriptableObjects/SOs singletons");
			}
			if (!DefaultPlantDataSOs.Instance.DefaultPlantDatas.Contains(this))
			{
				DefaultPlantDataSOs.Instance.DefaultPlantDatas.Add(this);
			}

		}

#endif
		InitializePool();
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
		SceneManager.sceneLoaded += OnSceneLoaded;
	}
	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{

		InitializePool();
	}


	public void InitializePool()
	{
		//if (PlantDictionary.ContainsKey(plantData.Name))
		//{
			//Logger.LogError("a DefaultPlantData Has the same name as another one name " + plantData.Name + " Please rename one of them to a different name");
		//}
		PlantDictionary[plantData.Name] = this;
	}


}