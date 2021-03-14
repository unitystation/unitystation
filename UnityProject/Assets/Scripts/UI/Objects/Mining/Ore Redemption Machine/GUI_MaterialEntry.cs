﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_MaterialEntry : DynamicEntry
{
	private GUI_MaterialsList materialList;

	private ItemTrait materialType;
	//private int currentAmount;

	public NetLabel labelName;
	public NetLabel labelAmount;
	public void DispenseMaterial(int amount)
	{
		materialList.materialStorageLink.usedStorage.DispenseSheet(amount, materialType, materialList.materialStorageLink.gameObject.WorldPosServer());
	}

	public void SetValues(ItemTrait material, int amount, GUI_MaterialsList matListDisplay)
	{
		materialList = matListDisplay;
		//currentAmount = amount;
		materialType = material;
		labelAmount.SetValueServer($"{amount} cm3");
		labelName.SetValueServer(material.name);
	}
}
