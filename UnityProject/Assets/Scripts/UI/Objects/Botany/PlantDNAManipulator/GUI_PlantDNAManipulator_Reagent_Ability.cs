using System.Linq;
using Items.Implants.Organs;
using Systems.Botany;
using TMPro;
using UI.Core.NetUI;
using UnityEngine;
using UnityEngine.UI;

public class GUI_PlantDNAManipulator_Reagent_Ability : DynamicEntry
{

	public NetText_label MainText;
	public NetText_label PercentageText;
	public NetUIChildActive ExtractButton;

	public ReagentNPercentage StoredReagentNPercentage;
	public PlantTrays StoredPlantTrays;

	public GUI_PlantDNAManipulator Parent;

	public void Remove()
	{
		if (Parent.PlantDNAManipulator.SeedPacket == null) return;
		if (StoredReagentNPercentage.ChemistryReagent != null)
		{
			Parent.PlantDNAManipulator.SeedPacket.plantData.ReagentProduction.RemoveAll(x=> x.ChemistryReagent == StoredReagentNPercentage.ChemistryReagent);
		}
		else
		{
			Parent.PlantDNAManipulator.SeedPacket.plantData.PlantTrays.Remove(StoredPlantTrays);
		}
		Parent.UpdateDisplay();
	}


	public void Interact()
	{
		if (Parent.PlantDNAManipulator.SeedPacket == null) return;
		if (Parent.PlantDNAManipulator.PlantDNADataDisc == null) return;

		if (Parent.PlantDNAManipulator.PlantDNADataDisc.IsEmpty())
		{
			if (StoredReagentNPercentage.ChemistryReagent != null)
			{
				Parent.PlantDNAManipulator.PlantDNADataDisc.LoadedData.ReagentProduction.Add(StoredReagentNPercentage);
			}
			else
			{
				Parent.PlantDNAManipulator.PlantDNADataDisc.LoadedData.PlantTrays.Add(StoredPlantTrays);
			}

			_ = Despawn.ServerSingle(Parent.PlantDNAManipulator.SeedPacket.gameObject);
		}
		else
		{
			if (Parent.PlantDNAManipulator.PlantDNADataDisc.LoadedData.ReagentProduction.Count > 0)
			{
				var InjectingReagent = Parent.PlantDNAManipulator.PlantDNADataDisc.LoadedData.ReagentProduction[0];
				if (Parent.PlantDNAManipulator.SeedPacket.plantData.ReagentProduction.Any(x => x.ChemistryReagent == InjectingReagent.ChemistryReagent))
				{
					return;
				}
				else
				{
					Parent.PlantDNAManipulator.SeedPacket.plantData.ReagentProduction.Add(InjectingReagent);
				}

			}
			else if (Parent.PlantDNAManipulator.PlantDNADataDisc.LoadedData.PlantTrays.Count > 0)
			{
				var InjectingPlantTrays = Parent.PlantDNAManipulator.PlantDNADataDisc.LoadedData.PlantTrays[0];
				if (Parent.PlantDNAManipulator.SeedPacket.plantData.PlantTrays.Contains(InjectingPlantTrays))
				{
					return;
				}
				else
				{
					Parent.PlantDNAManipulator.SeedPacket.plantData.PlantTrays.Add(InjectingPlantTrays);
				}
			}
		}
		Parent.UpdateDisplay();
	}

	public void SetupReagent(ReagentNPercentage ReagentNPercentage, bool ShowExtract,GUI_PlantDNAManipulator TParent)
	{
		Parent = TParent;
		this.StoredReagentNPercentage = ReagentNPercentage;
		MainText.MasterSetValue( ReagentNPercentage.ChemistryReagent.ToString());
		PercentageText.MasterSetValue((ReagentNPercentage.percentage * 100).ToString() + "%");
		if (ShowExtract)
		{
			ExtractButton.MasterNetSetActive(true);
		}
		else
		{
			ExtractButton.MasterNetSetActive(false);
		}
	}

	public void SetupAbility(PlantTrays PlantTrays, bool ShowExtract,GUI_PlantDNAManipulator TParent)
	{
		Parent = TParent;
		StoredPlantTrays = PlantTrays;
		MainText.MasterSetValue( PlantTrays.ToString());
		if (ShowExtract)
		{
			ExtractButton.MasterNetSetActive(true);
		}
		else
		{
			ExtractButton.MasterNetSetActive(false);
		}
	}

}
