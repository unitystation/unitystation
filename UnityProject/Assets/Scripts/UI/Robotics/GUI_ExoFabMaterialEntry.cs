using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_ExoFabMaterialEntry : DynamicEntry
{
	private GUI_ExosuitFabricator ExoFabMasterTab
	{
		get => MasterTab as GUI_ExosuitFabricator;
	}

	private ItemTrait materialType;

	public ItemTrait MaterialType
	{
		set => materialType = value;
	}

	private int currentAmount;

	public int CurrentAmount
	{
		set
		{
			currentAmount = value;
		}
	}

	private NetLabel amountLabel;
	public NetLabel AmountLabel { get => amountLabel; }

	private GUI_ExoFabButton buttonOne;
	private GUI_ExoFabButton buttonTen;
	private GUI_ExoFabButton buttonFifty;

	public void DispenseMaterial(int amount)
	{
		if (ExoFabMasterTab == null) ExoFabMasterTab.GetComponent<GUI_ExosuitFabricator>().OnDispenseSheetClicked.Invoke(amount, materialType);
		else { ExoFabMasterTab?.OnDispenseSheetClicked.Invoke(amount, materialType); }
	}

	public void ReInit(MaterialRecord materialRecord)
	{
		currentAmount = materialRecord.CurrentAmount;
		materialType = materialRecord.materialType;

		foreach (var element in Elements)
		{
			string nameBeforeIndex = element.name.Split('~')[0];
			switch (nameBeforeIndex)
			{
				case "MaterialName":
					element.SetValue = materialRecord.materialName + ":";
					break;

				case "MaterialAmount":
					element.SetValue = materialRecord.CurrentAmount.ToString() + " cm3";
					amountLabel = element as NetLabel;
					break;

				case "OneSheetButton":
					buttonOne = element as GUI_ExoFabButton;
					break;

				case "TenSheetButton":
					buttonTen = element as GUI_ExoFabButton;
					break;

				case "FiftySheetButton":
					buttonFifty = element as GUI_ExoFabButton;
					break;
			}
		}
		UpdateButtonVisibility();
	}

	public void UpdateButtonVisibility()
	{
		int sheetsDispensable = currentAmount / 2000;
		if (sheetsDispensable < 1)
		{
			buttonOne.SetValue = "false";
			buttonTen.SetValue = "false";
			buttonFifty.SetValue = "false";
		}
		else if (sheetsDispensable >= 1 && sheetsDispensable < 10)
		{
			buttonOne.SetValue = "true";
			buttonTen.SetValue = "false";
			buttonFifty.SetValue = "false";
		}
		else if (sheetsDispensable >= 10 && sheetsDispensable < 50)
		{
			buttonOne.SetValue = "true";
			buttonTen.SetValue = "true";
			buttonFifty.SetValue = "false";
		}
		else
		{
			buttonOne.SetValue = "true";
			buttonTen.SetValue = "true";
			buttonFifty.SetValue = "true";
		}
	}
}