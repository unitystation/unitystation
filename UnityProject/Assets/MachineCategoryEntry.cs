using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class MachineCategoryEntry : MonoBehaviour
{
	public NetLabel name;
	public ExoFabCategoryButton buttonCategory;
	public ExoFabCategoryButton buttonAddAll;

	public void Setup(string categoryName, MachineProduct[] machineProducts)
	{
		string formattedCategoryName = char.ToUpper(categoryName[0]) + categoryName.Substring(1).ToLower();
		this.gameObject.name = formattedCategoryName + "CategoryButton";

		name.SetValue = formattedCategoryName;
		name.gameObject.name = formattedCategoryName + "NameLabel";

		buttonCategory.gameObject.name = formattedCategoryName + "CategoryButton";
		buttonCategory.categoryName = categoryName;

		buttonAddAll.gameObject.name = formattedCategoryName + "AddAllButton";
	}
}