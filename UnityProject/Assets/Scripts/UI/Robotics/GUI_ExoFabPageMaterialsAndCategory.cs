using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExoFabPageMaterialsAndCategory : NetPage
{
	[SerializeField] private MaterialEntry materialEntry;
	private GameObject currentMaterialEntry;
	private ItemTrait currentMaterial;
	private string currentMaterialName;
	private SpawnResult spawnResult;
	private Dictionary<ItemTrait, MaterialEntry> materialEntries = new Dictionary<ItemTrait, MaterialEntry>();

	public void initMaterialList(ExosuitFabricator exofab)
	{
		//For each material record in the material storage, a new material entry is created in the
		//UI with the material type, its current amount and the dispense buttons.
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

	/// <summary>
	/// Updates the visibility of buttons for a given material.
	/// </summary>
	/// <param name="currentMaterialAmount"></param>
	/// <param name="cm3PerSheet">cm3 per material sheet. Standard is 2000cm3 per sheet</param>
	/// <param name="materialType"></param>
	public void UpdateButtonVisibility(int currentMaterialAmount, int cm3PerSheet, ItemTrait materialType)
	{
		materialEntries[materialType].SetButtonVisibility(cm3PerSheet, currentMaterialAmount);
	}

	/// <summary>
	/// Updates the material count for each material
	/// </summary>
	/// <param name="exofab"></param>
	public void UpdateMaterialCount(ExosuitFabricator exofab)
	{
		foreach (ItemTrait materialType in materialEntries.Keys)
		{
			string amountInExofab = exofab.materialStorage.ItemTraitToMaterialRecord[materialType].currentAmount.ToString();
			materialEntries[materialType].amountLabel.SetValue = amountInExofab;
		}
	}
}