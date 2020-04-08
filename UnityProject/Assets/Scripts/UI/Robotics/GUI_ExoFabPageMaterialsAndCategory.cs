using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExoFabPageMaterialsAndCategory : NetPage
{
	[SerializeField] private MaterialEntry materialEntry;
	[SerializeField] private MachineCategoryEntry categoryEntry;
	[SerializeField] private ExoFabProductEntry productEntryTemplate;
	[SerializeField] private GameObject productsParent;

	private Dictionary<ItemTrait, MaterialEntry> materialEntries = new Dictionary<ItemTrait, MaterialEntry>();

	public void InitMaterialList(ExosuitFabricator exofab)
	{
		ItemTrait currentMaterial = null;
		string currentMaterialName = null;
		GameObject currentMaterialEntry = null;
		SpawnResult spawnResult = null;
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

	public Dictionary<string, GameObject[]> InitCategoryList(MachineProductsCollection productsCollection, Dictionary<ItemTrait, string> materialToName)
	{
		SpawnResult spawnResult = null;
		Dictionary<string, GameObject[]> nameToProductEntries = new Dictionary<string, GameObject[]>();
		//For each loop creates a category entry with its products
		foreach (MachineProductCategory category in productsCollection.productCategoryList)
		{
			string categoryName = category.categoryName;
			spawnResult = Spawn.ServerPrefab(categoryEntry.gameObject,
				worldPosition: new Vector3(0, 0, 0), parent: this.transform.GetChild(2).GetChild(0).GetChild(0), count: 1);
			spawnResult.GameObject.GetComponent<MachineCategoryEntry>().Setup(categoryName, category.products);
			spawnResult.GameObject.SetActive(true);
			List<GameObject> productEntryList = new List<GameObject>();
			productEntryList = InitProductsList(category.products, categoryName, materialToName);
			nameToProductEntries.Add(categoryName, productEntryList.ToArray());
		}
		return nameToProductEntries;
	}

	public List<GameObject> InitProductsList(MachineProduct[] products, string categoryName, Dictionary<ItemTrait, string> materialToName)
	{
		SpawnResult spawnResult;
		List<GameObject> categoryProductEntries = new List<GameObject>();
		//Sets up a list of products, depending on what's in the collection of products
		foreach (MachineProduct product in products)
		{
			//Spawns one product entry for each one in this category
			spawnResult = Spawn.ServerPrefab(productEntryTemplate.gameObject,
				worldPosition: new Vector3(0, 0, 0), parent: productsParent.transform, count: 1);
			spawnResult.GameObject.GetComponent<ExoFabProductEntry>().Setup(product, materialToName);
			categoryProductEntries.Add(spawnResult.GameObject);
		}
		return categoryProductEntries;
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