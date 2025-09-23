using System.Collections.Generic;
using Systems.Botany;
using UI.Core.NetUI;
using UI.Objects.Medical;
using UnityEngine;

public class Reagent_Ability_List : EmptyItemList
{

	public GUI_PlantDNAManipulator_Reagent_Ability AddElementSetupAbility(PlantTrays PlantTrays, bool ShowExtract,GUI_PlantDNAManipulator TParent)
	{
		var NewElement  = AddItem() as GUI_PlantDNAManipulator_Reagent_Ability;
		NewElement.SetupAbility(PlantTrays, ShowExtract, TParent );
		return NewElement;
	}

	public GUI_PlantDNAManipulator_Reagent_Ability AddElementReagent(ReagentNPercentage ReagentNPercentage, bool ShowExtract,GUI_PlantDNAManipulator TParent)
	{
		var NewElement  = AddItem() as GUI_PlantDNAManipulator_Reagent_Ability;
		NewElement.SetupReagent(ReagentNPercentage,ShowExtract, TParent);
		return NewElement;
	}

	public List<GUI_PlantDNAManipulator_Reagent_Ability> GetElements()
	{

		List<GUI_PlantDNAManipulator_Reagent_Ability> ToReturn = new List<GUI_PlantDNAManipulator_Reagent_Ability>();

		foreach (var Entry in Entries)
		{
			ToReturn.Add(Entry as GUI_PlantDNAManipulator_Reagent_Ability);
		}

		return ToReturn;

	}
}
