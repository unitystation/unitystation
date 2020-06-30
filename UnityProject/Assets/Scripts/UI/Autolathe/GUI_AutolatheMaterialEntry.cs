using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_AutolatheMaterialEntry : DynamicEntry
{
	private GUI_Autolathe ExoFabMasterTab
	{
		get => MasterTab as GUI_Autolathe;
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
		if (ExoFabMasterTab == null) ExoFabMasterTab.GetComponent<GUI_Autolathe>().OnDispenseSheetClicked.Invoke(amount, materialType);
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
					((NetUIElement<string>)element).SetValueServer(materialRecord.materialName + ":");
					break;

				case "MaterialAmount":
					((NetUIElement<string>)element).SetValueServer(materialRecord.CurrentAmount + " cm3");
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
			buttonOne.SetValueServer("false");
			buttonTen.SetValueServer("false");
			buttonFifty.SetValueServer("false");
		}
		else if (sheetsDispensable >= 1 && sheetsDispensable < 10)
		{
			buttonOne.SetValueServer("true");
			buttonTen.SetValueServer("false");
			buttonFifty.SetValueServer("false");
		}
		else if (sheetsDispensable >= 10 && sheetsDispensable < 50)
		{
			buttonOne.SetValueServer("true");
			buttonTen.SetValueServer("true");
			buttonFifty.SetValueServer("false");
		}
		else
		{
			buttonOne.SetValueServer("true");
			buttonTen.SetValueServer("true");
			buttonFifty.SetValueServer("true");
		}
	}
}