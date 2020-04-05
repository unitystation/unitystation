using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExosuitFabricatorPageMaterialsAndCategory : GUI_ExosuitFabricatorPage
{
	[SerializeField] private NetLabel iron;
	[SerializeField] private NetLabel glass;
	[SerializeField] private NetLabel silver;
	[SerializeField] private NetLabel gold;
	[SerializeField] private NetLabel diamond;
	[SerializeField] private NetLabel plasma;
	[SerializeField] private NetLabel uranium;
	[SerializeField] private NetLabel bananium;
	[SerializeField] private NetLabel titanium;
	[SerializeField] private NetLabel bluespaceCrystal;
	[SerializeField] private NetLabel plastic;

	public void UpdateMaterialCount(ExosuitFabricator exofab)
	{
		iron.SetValue = SheetAmountToCubicCM(exofab.ironAmount).ToString() + "cm3";
		glass.SetValue = SheetAmountToCubicCM(exofab.glassAmount).ToString() + "cm3";
		silver.SetValue = SheetAmountToCubicCM(exofab.silverAmount).ToString() + "cm3";
		gold.SetValue = SheetAmountToCubicCM(exofab.goldAmount).ToString() + "cm3";
		diamond.SetValue = SheetAmountToCubicCM(exofab.diamondAmount).ToString() + "cm3";
		plasma.SetValue = SheetAmountToCubicCM(exofab.plasmaAmount).ToString() + "cm3";
		uranium.SetValue = SheetAmountToCubicCM(exofab.uraniumAmount).ToString() + "cm3";
		bananium.SetValue = SheetAmountToCubicCM(exofab.bananiumAmount).ToString() + "cm3";
		titanium.SetValue = SheetAmountToCubicCM(exofab.titaniumAmount).ToString() + "cm3";
		//bluespaceCrystal.SetValue = exofab.SheetAmountToCubicCM(bluespaceCrystalSheetAmount).ToString();+ "cm3";    not implemented yet as of April 4th 20
		plastic.SetValue = SheetAmountToCubicCM(exofab.plasticAmount).ToString() + "cm3";
		Logger.Log("Updating material count");
	}

	private int SheetAmountToCubicCM(int sheetAmount)
	{
		return sheetAmount;
	}
}