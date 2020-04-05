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

	[SerializeField] private GameObject ironRemoveOne;
	[SerializeField] private GameObject ironRemoveTen;
	[SerializeField] private GameObject ironRemoveFifty;
	[SerializeField] private GameObject glassRemoveOne;
	[SerializeField] private GameObject glassRemoveTen;
	[SerializeField] private GameObject glassRemoveFifty;
	[SerializeField] private GameObject silverRemoveOne;
	[SerializeField] private GameObject silverRemoveTen;
	[SerializeField] private GameObject silverRemoveFifty;
	[SerializeField] private GameObject goldRemoveOne;
	[SerializeField] private GameObject goldRemoveTen;
	[SerializeField] private GameObject goldRemoveFifty;
	[SerializeField] private GameObject diamondRemoveOne;
	[SerializeField] private GameObject diamondRemoveTen;
	[SerializeField] private GameObject diamondRemoveFifty;
	[SerializeField] private GameObject plasmaRemoveOne;
	[SerializeField] private GameObject plasmaRemoveTen;
	[SerializeField] private GameObject plasmaRemoveFifty;
	[SerializeField] private GameObject uraniumRemoveOne;
	[SerializeField] private GameObject uraniumRemoveTen;
	[SerializeField] private GameObject uraniumRemoveFifty;
	[SerializeField] private GameObject bananiumRemoveOne;
	[SerializeField] private GameObject bananiumRemoveTen;
	[SerializeField] private GameObject bananiumRemoveFifty;
	[SerializeField] private GameObject titaniumRemoveOne;
	[SerializeField] private GameObject titaniumRemoveTen;
	[SerializeField] private GameObject titaniumRemoveFifty;
	[SerializeField] private GameObject bluespaceCrystalRemoveOne;
	[SerializeField] private GameObject bluespaceCrystalRemoveTen;
	[SerializeField] private GameObject bluespaceCrystalRemoveFifty;
	[SerializeField] private GameObject plasticRemoveOne;
	[SerializeField] private GameObject plasticRemoveTen;
	[SerializeField] private GameObject plasticRemoveFifty;

	public void UpdateButtonVisibility(ExosuitFabricator exofab)
	{
		//Inefficient code, we can do better
		//if (exofab.ironAmount == 0)
		//{
		//	ironRemoveOne.SetActive(false);
		//}
		//else if (0 < exofab.ironAmount && exofab.ironAmount < 10)
		//{
		//	ironRemoveOne.SetActive(true);
		//}
		//else if (10 < exofab.ironAmount < 50)
	}

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
	}

	private int SheetAmountToCubicCM(int sheetAmount)
	{
		return sheetAmount;
	}
}