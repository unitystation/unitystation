using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialStorage : MonoBehaviour
{
	public int cm3PerSheet;
	public Dictionary<string, int> Stored;
	public int maximumTotalResourceStorage;
	private int currentTotalResourceAmount = 0;

	public MaterialsInMachineStorage materialsInMachines;
	public List<MaterialRecord> materialRecordList = new List<MaterialRecord>();
	public Dictionary<string, MaterialRecord> NameToMaterialRecord = new Dictionary<string, MaterialRecord>();
	public Dictionary<ItemTrait, string> MaterialToNameRecord = new Dictionary<ItemTrait, string>();
	public Dictionary<ItemTrait, MaterialRecord> ItemTraitToMaterialRecord = new Dictionary<ItemTrait, MaterialRecord>();

	private void Awake()
	{
		//2000cm3 per sheet is standard for ss13
		cm3PerSheet = materialsInMachines.cm3PerSheet;
		//Initializes the record of materials in the material storage.
		foreach (MaterialUsedInMachines material in materialsInMachines.materials)
		{
			MaterialRecord materialRecord = new MaterialRecord();
			materialRecord.currentAmount = 0;
			materialRecord.materialName = material.materialName;
			materialRecord.materialPrefab = material.materialPrefab;
			materialRecord.materialType = material.materialTrait;
			materialRecordList.Add(materialRecord);
		}

		//Optimizes retrieval of record
		foreach (MaterialRecord materialRecord in materialRecordList)
		{
			NameToMaterialRecord.Add(materialRecord.materialName.ToLower(), materialRecord);
			MaterialToNameRecord.Add(materialRecord.materialType, materialRecord.materialName);
			ItemTraitToMaterialRecord.Add(materialRecord.materialType, materialRecord);
		}
	}

	public bool TryAddMaterialCM3Value(string material, int quantity)
	{
		int valueInCM3 = quantity;
		int totalSum = valueInCM3 + currentTotalResourceAmount;
		if (totalSum <= maximumTotalResourceStorage)
		{
			NameToMaterialRecord[material.ToLower()].currentAmount += valueInCM3;
			currentTotalResourceAmount += valueInCM3;
			return true;
		}
		else return false;
	}

	public bool TryAddMaterialCM3Value(ItemTrait itemTrait, int quantity)
	{
		int valueInCM3 = quantity;
		int totalSum = valueInCM3 + currentTotalResourceAmount;
		if (totalSum <= maximumTotalResourceStorage)
		{
			ItemTraitToMaterialRecord[itemTrait].currentAmount += valueInCM3;
			currentTotalResourceAmount += valueInCM3;
			return true;
		}
		else return false;
	}

	public bool TryAddMaterialSheet(string material, int quantity)
	{
		int valueInCM3 = quantity * cm3PerSheet;
		int totalSum = valueInCM3 + currentTotalResourceAmount;
		if (totalSum <= maximumTotalResourceStorage)
		{
			NameToMaterialRecord[material.ToLower()].currentAmount += valueInCM3;
			currentTotalResourceAmount += valueInCM3;
			return true;
		}
		else return false;
	}

	public bool TryAddMaterialSheet(ItemTrait itemTrait, int quantity)
	{
		int valueInCM3 = quantity * cm3PerSheet;
		int totalSum = valueInCM3 + currentTotalResourceAmount;

		if (totalSum <= maximumTotalResourceStorage)
		{
			ItemTraitToMaterialRecord[itemTrait].currentAmount += valueInCM3;
			currentTotalResourceAmount += valueInCM3;
			return true;
		}
		else return false;
	}

	//Returns false if storage amount goes below 0
	public bool TryRemoveCM3Material(string material, int quantity)
	{
		int valueInCM3Removed = quantity;
		if (NameToMaterialRecord[material.ToLower()].currentAmount >= valueInCM3Removed)
		{
			NameToMaterialRecord[material.ToLower()].currentAmount -= valueInCM3Removed;
			currentTotalResourceAmount -= valueInCM3Removed;
			return true;
		}
		else return false;
	}

	public bool TryRemoveCM3Material(ItemTrait materialType, int quantity)
	{
		int valueInCM3Removed = quantity;
		if (ItemTraitToMaterialRecord[materialType].currentAmount >= valueInCM3Removed)
		{
			ItemTraitToMaterialRecord[materialType].currentAmount -= valueInCM3Removed;
			currentTotalResourceAmount -= valueInCM3Removed;
			return true;
		}
		else return false;
	}

	public bool TryRemoveMaterialSheet(string material, int quantity)
	{
		int valueInCM3Removed = quantity * cm3PerSheet;
		if (NameToMaterialRecord[material.ToLower()].currentAmount >= valueInCM3Removed)
		{
			NameToMaterialRecord[material.ToLower()].currentAmount -= valueInCM3Removed;
			currentTotalResourceAmount -= valueInCM3Removed;
			return true;
		}
		else return false;
	}

	public bool TryRemoveMaterialSheet(ItemTrait materialType, int quantity)
	{
		Logger.Log("TryRemoveMaterialSheet");
		int valueInCM3Removed = quantity * cm3PerSheet;
		if (ItemTraitToMaterialRecord[materialType].currentAmount >= valueInCM3Removed)
		{
			ItemTraitToMaterialRecord[materialType].currentAmount -= valueInCM3Removed;
			currentTotalResourceAmount -= valueInCM3Removed;
			return true;
		}
		else return false;
	}
}

public class MaterialRecord
{
	public int currentAmount { get; set; }
	public string materialName { get; set; }
	public ItemTrait materialType { get; set; }
	public GameObject materialPrefab { get; set; }
}