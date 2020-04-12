using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_MaterialEntry : MonoBehaviour
{
	public NetLabel nameLabel;
	public NetLabel amountLabel;

	public GUI_ExoFabRemoveMaterialButton buttonOne;
	public GUI_ExoFabRemoveMaterialButton buttonTen;
	public GUI_ExoFabRemoveMaterialButton buttonFifty;

	public void Setup(MaterialRecord materialRecord)
	{
		//    string materialName = char.ToUpper(materialRecord.materialName[0]) + materialRecord.materialName.Substring(1).ToLower();
		//    this.gameObject.name = materialName;

		//    nameLabel.gameObject.name = materialName + "NameLabel";
		//    nameLabel.SetValue = materialName;

		//    amountLabel.gameObject.name = materialName + "AmountLabel";
		//    amountLabel.SetValue = "0";

		//    buttonOne.value = 1;
		//    buttonOne.itemTrait = materialRecord.materialType;
		//    buttonOne.gameObject.name = "One" + materialName + "Button";

		//    buttonTen.value = 10;
		//    buttonTen.itemTrait = materialRecord.materialType;
		//    buttonTen.gameObject.name = "Ten" + materialName + "Button";

		//    buttonFifty.value = 50;
		//    buttonFifty.itemTrait = materialRecord.materialType;
		//    buttonFifty.gameObject.name = "Fifty" + materialName + "Button";
		//}

		//public void SetButtonVisibility(int cm3PerSheet, int materialAmount)
		//{
		//    int sheetsDispensable = materialAmount / cm3PerSheet;
		//    if (sheetsDispensable < 1)
		//    {
		//        buttonOne.gameObject.SetActive(false);
		//        buttonTen.gameObject.SetActive(false);
		//        buttonFifty.gameObject.SetActive(false);
		//    }
		//    else if (sheetsDispensable >= 1 && sheetsDispensable < 10)
		//    {
		//        buttonOne.gameObject.SetActive(true);
		//        buttonTen.gameObject.SetActive(false);
		//        buttonFifty.gameObject.SetActive(false);
		//    }
		//    else if (sheetsDispensable > 10 && sheetsDispensable < 50)
		//    {
		//        buttonOne.gameObject.SetActive(true);
		//        buttonTen.gameObject.SetActive(true);
		//        buttonFifty.gameObject.SetActive(false);
		//    }
		//    else
		//    {
		//        buttonOne.gameObject.SetActive(true);
		//        buttonTen.gameObject.SetActive(true);
		//        buttonFifty.gameObject.SetActive(true);
		//    }
		//}
	}
}