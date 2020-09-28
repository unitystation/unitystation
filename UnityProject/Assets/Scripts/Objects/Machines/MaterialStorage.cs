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
	private int maximumTotalResourceStorage = 0;

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
		//foreach (MaterialRecord materialRecord in materialRecordList)
		//{
		//	materialRecord.SyncCurrentAmount(materialRecord.CurrentAmount, materialRecord.CurrentAmount);
		//}
		//base.OnStartClient();
	}

	private void Awake()
	{
		EnsureInit();
	}

	public void EnsureInit()
	{
		//2000cm3 per sheet is standard for ss13
		cm3PerSheet = materialsInMachines.cm3PerSheet;
		//Initializes the record of materials in the material storage.
		materialRecordList.Clear();
		nameToMaterialRecord.Clear();
		ItemTraitToMaterialRecord.Clear();
		materialToNameRecord.Clear();
		foreach (MaterialSheet material in materialsInMachines.materials)
		{
			MaterialRecord materialRecord = new MaterialRecord();
			materialRecord.materialName = material.displayName;
			materialRecord.materialPrefab = material.RefinedPrefab;
			materialRecord.materialType = material.materialTrait;
			materialRecordList.Add(materialRecord);
		}

		//Optimizes retrieval of record
		foreach (MaterialRecord materialRecord in materialRecordList)
		{
			if (!MaterialToNameRecord.ContainsKey(materialRecord.materialType))
				MaterialToNameRecord.Add(materialRecord.materialType, materialRecord.materialName);
		}

		foreach (MaterialRecord materialRecord in materialRecordList)
		{
			if (!NameToMaterialRecord.ContainsKey(materialRecord.materialName))
			{
				NameToMaterialRecord.Add(materialRecord.materialName.ToLower(), materialRecord);
			}
			if (!ItemTraitToMaterialRecord.ContainsKey(materialRecord.materialType))
			{
				ItemTraitToMaterialRecord.Add(materialRecord.materialType, materialRecord);
			}
		}
	}

	/// <summary>
	/// Attempt to add material sheets to the storage, these are converted into cm3. Returns true on success and false on failure.
	/// </summary>
	[Server]
	public bool TryAddMaterialSheet(ItemTrait itemTrait, int quantity)
	{
		int valueInCM3 = quantity * cm3PerSheet;
		int totalSum = valueInCM3 + currentTotalResourceAmount;

		if (totalSum <= maximumTotalResourceStorage)
		{
			//Sets the current amount of a certain material
			int newAmount = ItemTraitToMaterialRecord[itemTrait].CurrentAmount + valueInCM3;
			ItemTraitToMaterialRecord[itemTrait].SetCurrentAmount(newAmount);
			//Sets the total amount of all materials
			int newTotalAmount = CurrentTotalResourceAmount + valueInCM3;
			ServerSetCurrentTotalResourceAmount(newTotalAmount);
			return true;
		}
		else return false;
	}

	/// <summary>
	/// Attempt to remove a cm3 amount of materials from the storage. Returns true on success and false on failure.
	/// </summary>
	[Server]
	public bool TryRemoveCM3Material(ItemTrait materialType, int quantity)
	{
		int valueInCM3Removed = quantity;
		if (ItemTraitToMaterialRecord[materialType].CurrentAmount >= valueInCM3Removed)
		{
			int newAmount = ItemTraitToMaterialRecord[materialType].CurrentAmount - valueInCM3Removed;
			ItemTraitToMaterialRecord[materialType].SetCurrentAmount(newAmount);
			return true;
		}
		else return false;
	}

	/// <summary>
	/// Attempt to remove an amount of material sheets from the storage. Returns true on success and false on failure.
	/// </summary>
	[Server]
	public bool TryRemoveMaterialSheet(ItemTrait materialType, int quantity)
	{
		int valueInCM3Removed = quantity * cm3PerSheet;
		if (ItemTraitToMaterialRecord[materialType].CurrentAmount >= valueInCM3Removed)
		{
			//Sets the new current amount of material
			int newAmount = ItemTraitToMaterialRecord[materialType].CurrentAmount - valueInCM3Removed;
			ItemTraitToMaterialRecord[materialType].SetCurrentAmount(newAmount);

			//Sets the total amount of all materials
			int newTotalAmount = CurrentTotalResourceAmount - valueInCM3Removed;
			ServerSetCurrentTotalResourceAmount(newTotalAmount);
			return true;
		}
		else return false;
	}

	/// <summary>
	/// Attempt to remove an amount of materials from a Dictionary of materials. Returns true on success and false on failure.
	/// </summary>
	[Server]
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
			ItemTraitToMaterialRecord[material.materialTrait].SetCurrentAmount(newAmount);
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
		EnsureInit();
		ResetMaterialStorage();
	}

	public void ResetMaterialStorage()
	{
		ServerSetCurrentTotalResourceAmount(0);
		foreach (MaterialRecord materialRecord in ItemTraitToMaterialRecord.Values)
		{
			materialRecord.SetCurrentAmount(0);
		}
	}
}

public class MaterialRecord
{
	private int currentAmount;

	public int CurrentAmount { get => currentAmount; }
	public string materialName { get; set; }
	public ItemTrait materialType { get; set; }
	public GameObject materialPrefab { get; set; }

	public void SetCurrentAmount(int newValue)
	{
		currentAmount = newValue;
	}
}