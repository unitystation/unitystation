using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MaterialStorage : NetworkBehaviour, IServerSpawn
{
	public MaterialsInMachineStorage materialsInMachines;
	private int cm3PerSheet;
	public int CM3PerSheet { get => cm3PerSheet; }

	[SerializeField]
	private int maximumTotalResourceStorage;

	[SyncVar(hook = nameof(SyncCurrentTotalResourceAmount))]
	private int currentTotalResourceAmount = 0;

	public int CurrentTotalResourceAmount { get => currentTotalResourceAmount; }

	private List<MaterialRecord> materialRecordList = new List<MaterialRecord>();

	public List<MaterialRecord> MaterialRecordList { get => materialRecordList; }
	private Dictionary<string, MaterialRecord> nameToMaterialRecord = new Dictionary<string, MaterialRecord>();

	public Dictionary<string, MaterialRecord> NameToMaterialRecord { get => nameToMaterialRecord; }

	private static Dictionary<ItemTrait, string> materialToNameRecord = new Dictionary<ItemTrait, string>();
	public static Dictionary<ItemTrait, string> MaterialToNameRecord { get => materialToNameRecord; }

	private Dictionary<ItemTrait, MaterialRecord> itemTraitToMaterialRecord = new Dictionary<ItemTrait, MaterialRecord>();
	public Dictionary<ItemTrait, MaterialRecord> ItemTraitToMaterialRecord { get => itemTraitToMaterialRecord; }

	public override void OnStartClient()
	{
		foreach (MaterialRecord materialRecord in materialRecordList)
		{
			materialRecord.SyncCurrentAmount(materialRecord.CurrentAmount, materialRecord.CurrentAmount);
		}
		base.OnStartClient();
	}

	private void Awake()
	{
		//2000cm3 per sheet is standard for ss13
		cm3PerSheet = materialsInMachines.cm3PerSheet;
		//Initializes the record of materials in the material storage.
		foreach (MaterialSheet material in materialsInMachines.materials)
		{
			MaterialRecord materialRecord = new MaterialRecord();
			materialRecord.materialName = material.displayName;
			materialRecord.materialPrefab = material.RefinedPrefab;
			materialRecord.materialType = material.materialTrait;
			materialRecordList.Add(materialRecord);
		}

		//Temporary solution to get material name and material type together, metal is called iron in machines for instance etc.
		foreach (MaterialRecord materialRecord in materialRecordList)
		{
			if (!MaterialToNameRecord.ContainsKey(materialRecord.materialType))
				MaterialToNameRecord.Add(materialRecord.materialType, materialRecord.materialName);
		}

		//Optimizes retrieval of record
		foreach (MaterialRecord materialRecord in materialRecordList)
		{
			NameToMaterialRecord.Add(materialRecord.materialName.ToLower(), materialRecord);
			ItemTraitToMaterialRecord.Add(materialRecord.materialType, materialRecord);
		}
	}

	//public bool TryAddMaterialCM3Value(string material, int quantity)
	//{
	//	int valueInCM3 = quantity;
	//	int totalSum = valueInCM3 + currentTotalResourceAmount;
	//	if (totalSum <= maximumTotalResourceStorage)
	//	{
	//		NameToMaterialRecord[material.ToLower()].currentAmount += valueInCM3;
	//		currentTotalResourceAmount += valueInCM3;
	//		return true;
	//	}
	//	else return false;
	//}

	//public bool TryAddMaterialCM3Value(ItemTrait itemTrait, int quantity)
	//{
	//	int valueInCM3 = quantity;
	//	int totalSum = valueInCM3 + currentTotalResourceAmount;
	//	if (totalSum <= maximumTotalResourceStorage)
	//	{
	//		ItemTraitToMaterialRecord[itemTrait].currentAmount += valueInCM3;
	//		currentTotalResourceAmount += valueInCM3;
	//		return true;
	//	}
	//	else return false;
	//}

	//public bool TryAddMaterialSheet(string material, int quantity)
	//{
	//	int valueInCM3 = quantity * cm3PerSheet;
	//	int totalSum = valueInCM3 + currentTotalResourceAmount;
	//	if (totalSum <= maximumTotalResourceStorage)
	//	{
	//		NameToMaterialRecord[material.ToLower()].currentAmount += valueInCM3;
	//		currentTotalResourceAmount += valueInCM3;
	//		return true;
	//	}
	//	else return false;
	//}

	[Server]
	public bool TryAddMaterialSheet(ItemTrait itemTrait, int quantity)
	{
		int valueInCM3 = quantity * cm3PerSheet;
		int totalSum = valueInCM3 + currentTotalResourceAmount;

		if (totalSum <= maximumTotalResourceStorage)
		{
			//Sets the current amount of a certain material
			int newAmount = ItemTraitToMaterialRecord[itemTrait].CurrentAmount + valueInCM3;
			ItemTraitToMaterialRecord[itemTrait].ServerSetCurrentAmount(newAmount);
			//Sets the total amount of all materials
			int newTotalAmount = CurrentTotalResourceAmount + valueInCM3;
			ServerSetCurrentTotalResourceAmount(newTotalAmount);
			return true;
		}
		else return false;
	}

	//public bool TryRemoveCM3Material(string material, int quantity)
	//{
	//	int valueInCM3Removed = quantity;
	//	if (NameToMaterialRecord[material.ToLower()].currentAmount >= valueInCM3Removed)
	//	{
	//		NameToMaterialRecord[material.ToLower()].currentAmount -= valueInCM3Removed;
	//		currentTotalResourceAmount -= valueInCM3Removed;
	//		return true;
	//	}
	//	else return false;
	//}

	[Server]
	public bool TryRemoveCM3Material(ItemTrait materialType, int quantity)
	{
		int valueInCM3Removed = quantity;
		if (ItemTraitToMaterialRecord[materialType].CurrentAmount >= valueInCM3Removed)
		{
			int newAmount = ItemTraitToMaterialRecord[materialType].CurrentAmount - valueInCM3Removed;
			ItemTraitToMaterialRecord[materialType].ServerSetCurrentAmount(newAmount);
			return true;
		}
		else return false;
	}

	//public bool TryRemoveMaterialSheet(string material, int quantity)
	//{
	//	int valueInCM3Removed = quantity * cm3PerSheet;
	//	if (NameToMaterialRecord[material.ToLower()].currentAmount >= valueInCM3Removed)
	//	{
	//		NameToMaterialRecord[material.ToLower()].currentAmount -= valueInCM3Removed;
	//		currentTotalResourceAmount -= valueInCM3Removed;
	//		return true;
	//	}
	//	else return false;
	//}

	[Server]
	public bool TryRemoveMaterialSheet(ItemTrait materialType, int quantity)
	{
		int valueInCM3Removed = quantity * cm3PerSheet;
		if (ItemTraitToMaterialRecord[materialType].CurrentAmount >= valueInCM3Removed)
		{
			//Sets the new current amount of material
			int newAmount = ItemTraitToMaterialRecord[materialType].CurrentAmount - valueInCM3Removed;
			ItemTraitToMaterialRecord[materialType].ServerSetCurrentAmount(newAmount);

			//Sets the total amount of all materials
			int newTotalAmount = CurrentTotalResourceAmount - valueInCM3Removed;
			ServerSetCurrentTotalResourceAmount(newTotalAmount);
			return true;
		}
		else return false;
	}

	public bool TryRemoveCM3Materials(DictionaryMaterialToIntAmount materialsAndAmount)
	{
		//Checks if the materials from a list of materials and amount can be removed from the storage without going below 0
		foreach (MaterialSheet material in materialsAndAmount.Keys)
		{
			int amountInStorage = ItemTraitToMaterialRecord[material.materialTrait].CurrentAmount;
			int productAmountCost = materialsAndAmount[material];
			if (amountInStorage < productAmountCost)
			{
				return false;
			}
		}

		int newTotalResourceAmount = CurrentTotalResourceAmount;
		//Removes all the materials and their amount from the storage.
		foreach (MaterialSheet material in materialsAndAmount.Keys)
		{
			int amountInStorage = ItemTraitToMaterialRecord[material.materialTrait].CurrentAmount;
			int productAmountCost = materialsAndAmount[material];
			int newAmount = amountInStorage - productAmountCost;
			newTotalResourceAmount -= productAmountCost;
			ItemTraitToMaterialRecord[material.materialTrait].ServerSetCurrentAmount(newAmount);
		}
		ServerSetCurrentTotalResourceAmount(newTotalResourceAmount);
		return true;
	}

	[Server]
	public void ServerSetCurrentTotalResourceAmount(int newValue)
	{
		int oldTotalAmount = currentTotalResourceAmount;
		currentTotalResourceAmount = newValue;
		SyncCurrentTotalResourceAmount(oldTotalAmount, currentTotalResourceAmount);
	}

	public void SyncCurrentTotalResourceAmount(int oldValue, int newValue)
	{
		currentTotalResourceAmount = newValue;
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		throw new System.NotImplementedException();
	}
}

public class MaterialRecord : NetworkBehaviour
{
	[SyncVar(hook = nameof(SyncCurrentAmount))]
	private int currentAmount;

	public int CurrentAmount { get => currentAmount; }
	public string materialName { get; set; }
	public ItemTrait materialType { get; set; }
	public GameObject materialPrefab { get; set; }

	[Server]
	public void ServerSetCurrentAmount(int newAmount)
	{
		int oldAmount = CurrentAmount;
		currentAmount = newAmount;
		SyncCurrentAmount(oldAmount, currentAmount);
	}

	public void SyncCurrentAmount(int oldValue, int newValue)
	{
		currentAmount = newValue;
	}
}