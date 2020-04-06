using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExosuitFabricatorPageMaterialsAndCategory : GUI_ExosuitFabricatorPage
{
	[SerializeField] private MaterialEntry materialEntry;
	private GameObject currentMaterialEntry;
	private ItemTrait currentMaterial;
	private string currentMaterialName;
	private SpawnResult spawnResult;
	private Dictionary<ItemTrait, MaterialEntry> materialEntries = new Dictionary<ItemTrait, MaterialEntry>();

	public void initMaterialList(ExosuitFabricator exofab)
	{
		foreach (MaterialRecord record in exofab.materialStorage.ItemTraitToMaterialRecord.Values)
		{
			currentMaterial = record.materialType;
			currentMaterialName = record.materialName;
			spawnResult = Spawn.ServerPrefab(materialEntry.gameObject,
				worldPosition: new Vector3(0, 0, 0), parent: this.transform.GetChild(0), count: 1);
			spawnResult.GameObject.GetComponent<MaterialEntry>().Setup(record);
			spawnResult.GameObject.SetActive(true);
			currentMaterialEntry = spawnResult.GameObject;
			materialEntries.Add(currentMaterial, spawnResult.GameObject.GetComponent<MaterialEntry>());
		}
	}

	public void UpdateButtonVisibility(int currentValue, ItemTrait materialType)
	{
		//Inefficient code, we can do better
		//if (exofab.ironAmount == 0)
		//{
		//	ironRemoveOne.SetActive(false);
		//}
		//else if (0 < exofab.ironAmount && exofab.ironAmount < 10)
		//{
		//	ironRemoveOne.SetActive(true);
		//}
		//else if (10 < exofab.ironAmount < 50)
	}

	public void UpdateMaterialCount(ExosuitFabricator exofab)
	{
		foreach (ItemTrait materialType in materialEntries.Keys)
		{
			Logger.Log("UPDATING MATERIALS FOR: " + materialType.name);
			string amountInExofab = exofab.materialStorage.ItemTraitToMaterialRecord[materialType].currentAmount.ToString();
			materialEntries[materialType].amountLabel.SetValue = amountInExofab;
		}
	}
}