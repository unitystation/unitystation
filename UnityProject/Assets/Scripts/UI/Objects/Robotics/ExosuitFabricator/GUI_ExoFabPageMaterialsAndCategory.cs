using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Machines;

namespace UI.Objects.Robotics
{
	public class GUI_ExoFabPageMaterialsAndCategory : NetPage
	{
		[SerializeField] private EmptyItemList materialList = null;
		[SerializeField] private EmptyItemList productCategoryList = null;

		public void InitMaterialList(MaterialStorage materialStorage)
		{
			var materialRecords = materialStorage.MaterialList;

			materialList.Clear();
			materialList.AddItems(materialRecords.Count);
			var i = 0;
			foreach (var material in materialRecords.Keys)
			{
				GUI_ExoFabMaterialEntry item = materialList.Entries[i] as GUI_ExoFabMaterialEntry;
				item.ReInit(material, materialRecords[material]);
				i++;
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
		public void UpdateMaterialList(MaterialStorage materialStorage)
		{
			InitMaterialList(materialStorage);
		}
	}
}
