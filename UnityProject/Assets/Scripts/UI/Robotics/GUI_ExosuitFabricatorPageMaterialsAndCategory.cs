using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExosuitFabricatorPageMaterialsAndCategory : GUI_ExosuitFabricatorPage
{
    [SerializeField] private NetLabel iron;
    [SerializeField] private NetLabel glass;
    [SerializeField] private NetLabel silver;
    [SerializeField] private NetLabel diamond;
    [SerializeField] private NetLabel plasma;
    [SerializeField] private NetLabel uranium;
    [SerializeField] private NetLabel bananium;
    [SerializeField] private NetLabel titanium;
    [SerializeField] private NetLabel bluespaceCrystal;
    [SerializeField] private NetLabel plastic;

    public void UpdateMaterialCount(ExosuitFabricator exofab)
    {
        iron.SetValue = SheetAmountToCubicCM(exofab.metalSheetAmount).ToString() + "cm3";
        glass.SetValue = SheetAmountToCubicCM(exofab.glassSheetAmount).ToString() + "cm3";
        silver.SetValue = SheetAmountToCubicCM(exofab.silverSheetAmount).ToString() + "cm3";
        diamond.SetValue = SheetAmountToCubicCM(exofab.diamondSheetAmount).ToString() + "cm3";
        plasma.SetValue = SheetAmountToCubicCM(exofab.plasmaSheetAmount).ToString() + "cm3";
        uranium.SetValue = SheetAmountToCubicCM(exofab.uraniumSheetAmount).ToString() + "cm3";
        bananium.SetValue = SheetAmountToCubicCM(exofab.bananiumSheetAmount).ToString() + "cm3";
        titanium.SetValue = SheetAmountToCubicCM(exofab.titaniumSheetAmount).ToString() + "cm3";
        //bluespaceCrystal.SetValue = exofab.SheetAmountToCubicCM(bluespaceCrystalSheetAmount).ToString();+ "cm3";    not implemented yet as of April 4th 20
        plastic.SetValue = SheetAmountToCubicCM(exofab.plasticSheetAmount).ToString() + "cm3";
        Logger.Log("Updating material count");
    }

    private int SheetAmountToCubicCM(int sheetAmount)
    {
        return sheetAmount * 2000;
    }
}