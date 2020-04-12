using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExoFabPageMaterialsAndCategory : NetPage
{
	[SerializeField] private EmptyItemList materialList;
	[SerializeField] private EmptyItemList productCategoryList;

	//private Dictionary<ItemTrait, GUI_ExoFabMaterialEntry> materialEntries = new Dictionary<ItemTrait, GUI_ExoFabMaterialEntry>();

	public void InitMaterialList(MaterialStorage materialStorage)
	{
		List<MaterialRecord> materialRecords = materialStorage.MaterialRecordList;

		materialList.Clear();
		materialList.AddItems(materialRecords.Count);
		for (int i = 0; i < materialRecords.Count; i++)
		{
			GUI_ExoFabMaterialEntry item = materialList.Entries[i] as GUI_ExoFabMaterialEntry;
			item.ReInit(materialRecords[i]);
			//materialEntries.Add(materialRecords[i].materialType, item);
		}
	}

	public void InitCategories(MachineProductsCollection exoFabProducts)
	{
		List<MachineProductList> categories = exoFabProducts.ProductCategoryList;

		productCategoryList.Clear();
		productCategoryList.AddItems(categories.Count);
		for (int i = 0; i < categories.Count; i++)
		{
			GUI_ExoFabCategoryEntry item = productCategoryList.Entries[i] as GUI_ExoFabCategoryEntry;
			item.ExoFabProducts = categories[i];
			item.ReInit(categories[i]);
		}
	}

	/// <summary>
	/// Updates the material count for each material
	/// </summary>
	/// <param name="exofab"></param>
	public void UpdateMaterialCount(MaterialStorage materialStorage)
	{
		InitMaterialList(materialStorage);
		//GUI_ExoFabMaterialEntry materialEntry;
		//foreach (MaterialRecord materialRecord in materialStorage.MaterialRecordList)
		//{
		//	string currentAmountInString = materialRecord.CurrentAmount.ToString();
		//	materialEntry = materialEntries[materialRecord.materialType];
		//	materialEntry.AmountLabel.SetValue = currentAmountInString;
		//	materialEntry.CurrentAmount = materialRecord.CurrentAmount;
		//	materialEntry.UpdateButtonVisibility();
		//}
	}
}