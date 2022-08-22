using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Machines;

namespace UI.Objects
{
	public class GUI_AutolathePageMaterialsAndCategory : NetPage
	{
		[SerializeField] private EmptyItemList materialList = null;
		[SerializeField] private EmptyItemList productCategoryList = null;
		[SerializeField] private EmptyItemList productCategoryListSecondRow = null;
		private int numberOfCategoriesInFirstColumn = 0;
		private int numberOfCategoriesInSecondColumn = 0;

		public void InitMaterialList(MaterialStorage materialStorage)
		{
			var materialRecords = materialStorage.MaterialList;

			materialList.Clear();
			materialList.AddItems(materialRecords.Count);
			var i = 0;
			foreach (var material in materialRecords.Keys)
			{
				GUI_AutolatheMaterialEntry item = materialList.Entries[i] as GUI_AutolatheMaterialEntry;
				item.ReInit(material, materialRecords[material]);
				i++;
			}
		}

		public void InitCategories(MachineProductsCollection exoFabProducts)
		{
			List<MachineProductList> categories = exoFabProducts.ProductCategoryList;

			productCategoryListSecondRow.Clear();
			productCategoryList.Clear();

			//Checks the amount of categories that should be in first column and second column
			if (categories.Count % 2 == 0)
			{
				numberOfCategoriesInFirstColumn = categories.Count / 2;
				numberOfCategoriesInSecondColumn = numberOfCategoriesInFirstColumn;
			}
			else
			{
				numberOfCategoriesInFirstColumn = categories.Count / 2 + 1;
				numberOfCategoriesInSecondColumn = numberOfCategoriesInFirstColumn - 1;
			}
			productCategoryList.AddItems(numberOfCategoriesInFirstColumn);
			productCategoryListSecondRow.AddItems(numberOfCategoriesInSecondColumn);

			for (int i = 0; i < numberOfCategoriesInFirstColumn; i++)
			{
				GUI_AutolatheCategoryEntry item = productCategoryList.Entries[i] as GUI_AutolatheCategoryEntry;
				item.ReInit(categories[i]);
			}

			for (int i = 0; i < numberOfCategoriesInSecondColumn; i++)
			{
				GUI_AutolatheCategoryEntry item = productCategoryListSecondRow.Entries[i] as GUI_AutolatheCategoryEntry;
				item.ReInit(categories[i + numberOfCategoriesInFirstColumn]);
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
