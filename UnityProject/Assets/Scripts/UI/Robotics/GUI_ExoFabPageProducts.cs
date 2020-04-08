using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExoFabPageProducts : NetPage

{
	public ExoFabProductButton returnButton;

	//Sets the products of the category button active.
	public void SetupPage(ExoFabCategoryButton button, Dictionary<string, GameObject[]> nameToProductEntries)
	{
		foreach (GameObject productEntry in nameToProductEntries[button.categoryName])
		{
			productEntry.SetActive(true);
		}

		//Gives the return button the reference for the product entries, so they can be disabled when pressed.
		returnButton.categoryName = button.categoryName;
	}
}