using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExoFabMaterialEntry : DynamicEntry
{
	private GUI_ExosuitFabricator ExoFabMasterTab = null;

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

	private NetButton buttonOne;
	private NetButton buttonTen;
	private NetButton buttonFifty;

	public void DispenseMaterial(int amount)
	{
		Logger.Log("CLICK");
		if (ExoFabMasterTab == null)
		{
			MasterTab.GetComponent<GUI_ExosuitFabricator>().OnDispenseSheetClicked.Invoke(amount, materialType);
		}
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
					element.SetValue = materialRecord.materialName;
					break;

				case "MaterialAmount":
					element.SetValue = materialRecord.CurrentAmount.ToString();
					amountLabel = element as NetLabel;
					break;

				case "OneSheetButton":
					buttonOne = element as NetButton;
					break;

				case "TenSheetButton":
					buttonTen = element as NetButton;
					break;

				case "FiftySheetButton":
					buttonFifty = element as NetButton;
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
			buttonOne.gameObject.SetActive(false);
			buttonTen.gameObject.SetActive(false);
			buttonFifty.gameObject.SetActive(false);
		}
		else if (sheetsDispensable >= 1 && sheetsDispensable < 10)
		{
			buttonOne.gameObject.SetActive(true);
			buttonTen.gameObject.SetActive(false);
			buttonFifty.gameObject.SetActive(false);
		}
		else if (sheetsDispensable > 10 && sheetsDispensable < 50)
		{
			buttonOne.gameObject.SetActive(true);
			buttonTen.gameObject.SetActive(true);
			buttonFifty.gameObject.SetActive(false);
		}
		else
		{
			buttonOne.gameObject.SetActive(true);
			buttonTen.gameObject.SetActive(true);
			buttonFifty.gameObject.SetActive(true);
		}
	}
}