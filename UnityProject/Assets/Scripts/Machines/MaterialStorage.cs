using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialStorage : MonoBehaviour
{
	public Dictionary<string, int> Stored;
	public int maximumTotalResourceStorage;
	private int currentTotalResourceAmount;

	public MaterialsInMachineStorage materialsInMachines;
	public List<MaterialRecord> materialRecordList = new List<MaterialRecord>();
	public Dictionary<string, MaterialRecord> NameToMaterialRecord = new Dictionary<string, MaterialRecord>();
	public Dictionary<ItemTrait, MaterialRecord> ItemTraitToMaterialRecord = new Dictionary<ItemTrait, MaterialRecord>();

	private int cm3PerSheet;

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
			Logger.Log("ADDING TO LIST: " + materialRecord.materialName);
		}

		//Optimizes retrieval of record
		foreach (MaterialRecord materialRecord in materialRecordList)
		{
			NameToMaterialRecord.Add(materialRecord.materialName, materialRecord);
			ItemTraitToMaterialRecord.Add(materialRecord.materialType, materialRecord);
		}
	}

	public bool TryAddMaterialValue(string material, int quantity)
	{
		int valueInCM3 = quantity;
		int totalSum = valueInCM3 + currentTotalResourceAmount;
		if (totalSum <= maximumTotalResourceStorage)
		{
			NameToMaterialRecord[material.ToLower()].currentAmount = totalSum;
			Logger.Log("QUANTITY ADDED: " + quantity + "  TOTAL SUM: " + totalSum);
			return true;
		}
		else return false;
	}

	public bool TryAddMaterialValue(ItemTrait itemTrait, int quantity)
	{
		int valueInCM3 = quantity;
		int totalSum = valueInCM3 + currentTotalResourceAmount;
		if (totalSum <= maximumTotalResourceStorage)
		{
			ItemTraitToMaterialRecord[itemTrait].currentAmount = totalSum;
			Logger.Log("QUANTITY ADDED: " + quantity + "  TOTAL SUM: " + totalSum);
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
			NameToMaterialRecord[material.ToLower()].currentAmount = totalSum;
			Logger.Log("QUANTITY ADDED: " + quantity + "  TOTAL SUM: " + totalSum);
			return true;
		}
		else return false;
	}

	public bool TryAddMaterialSheet(ItemTrait itemTrait, int quantity)
	{
		Logger.Log("Try add materialSheet");
		int valueInCM3 = quantity * cm3PerSheet;
		int totalSum = valueInCM3 + currentTotalResourceAmount;
		Logger.Log("VALUEINCM3 = " + valueInCM3);
		Logger.Log("totalSum = " + totalSum);
		if (totalSum <= maximumTotalResourceStorage)
		{
			ItemTraitToMaterialRecord[itemTrait].currentAmount = totalSum;
			Logger.Log("QUANTITY ADDED: " + quantity + "  TOTAL SUM: " + totalSum);
			return true;
		}
		else return false;
	}

	//Returns false if storage amount goes below 0
	public bool TryRemoveMaterial(string material, int quantity)
	{
		int valueInCM3Removed = quantity * cm3PerSheet;
		if (NameToMaterialRecord[material.ToLower()].currentAmount >= valueInCM3Removed)
		{
			NameToMaterialRecord[material.ToLower()].currentAmount -= valueInCM3Removed;
			Logger.Log("QUANTITY REMOVED: " + quantity + "  TOTAL SUM: " + NameToMaterialRecord[material.ToLower()].currentAmount);
			return true;
		}
		else return false;
	}

	public bool TryRemoveMaterial(ItemTrait materialType, int quantity)
	{
		int valueInCM3Removed = quantity * cm3PerSheet;
		if (ItemTraitToMaterialRecord[materialType].currentAmount >= valueInCM3Removed)
		{
			ItemTraitToMaterialRecord[materialType].currentAmount -= valueInCM3Removed;
			Logger.Log("QUANTITY REMOVED: " + quantity + "  TOTAL SUM: " + ItemTraitToMaterialRecord[materialType].currentAmount);
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
			Logger.Log("QUANTITY REMOVED: " + quantity + "  TOTAL SUM: " + NameToMaterialRecord[material.ToLower()].currentAmount);
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
			Logger.Log("QUANTITY REMOVED: " + quantity + "  TOTAL SUM: " + ItemTraitToMaterialRecord[materialType].currentAmount);
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