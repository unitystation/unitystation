using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Core.NetUI;
using UnityEngine;

public class GUI_PlantDNAManipulator : NetTab
{
	//TODO
	//look at Enabling and disabling Causing havoc with interactions

	public PlantDNAManipulator PlantDNAManipulator;

	public NetText_label SeedText;
	public NetText_label CartridgeText;


	public NetText_label Potency;
	public NetText_label Yield;
	public NetText_label ProductionSpeed;
	public NetText_label endurance;
	public NetText_label Lifespan;
	public NetText_label WeedResistance;
	public NetText_label WeedGrowthRate;


	public NetText_label PotencyBT;
	public NetText_label YieldBT;
	public NetText_label ProductionSpeedBT;
	public NetText_label enduranceBT;
	public NetText_label LifespanBT;
	public NetText_label WeedResistanceBT;
	public NetText_label WeedGrowthRateBT;


	public NetUIChildActive PotencyB;
	public NetUIChildActive YieldB;
	public NetUIChildActive ProductionSpeedB;
	public NetUIChildActive enduranceB;
	public NetUIChildActive LifespanB;
	public NetUIChildActive WeedResistanceB;
	public NetUIChildActive WeedGrowthRateB;


	public GUI_PlantDNAManipulator_Reagent_Ability ReagentPrefab;
	public GUI_PlantDNAManipulator_Reagent_Ability AbilityPrefab;

	public NetUIChildActive ReagentInjectobject;
	public NetUIChildActive AbilityInjectobject;

	public NetText_label ReagentInjectText;
	public NetText_label AbilityInjectText;


	public Reagent_Ability_List ReagentsSynchronousList;
	public Reagent_Ability_List AbilitysSynchronousList;


	private void Start()
	{
		if (Provider != null)
		{
			PlantDNAManipulator = Provider.GetComponentInChildren<PlantDNAManipulator>();
			PlantDNAManipulator.RegisterConsoleGUI(this);
		}

		UpdateDisplay();
	}

	public void UpdateDisplay()
	{
		ReagentsSynchronousList.Clear();
		AbilitysSynchronousList.Clear();


		bool ShowExtract = false;

		if (PlantDNAManipulator.PlantDNADataDisc != null)
		{
			CartridgeText.MasterSetValue("Eject");
			ShowExtract = PlantDNAManipulator.PlantDNADataDisc.IsEmpty();

			if (ShowExtract && PlantDNAManipulator.SeedPacket != null)
			{
				PotencyB.MasterNetSetActive(true);
				YieldB.MasterNetSetActive(true);
				ProductionSpeedB.MasterNetSetActive(true);
				enduranceB.MasterNetSetActive(true);
				LifespanB.MasterNetSetActive(true);
				WeedResistanceB.MasterNetSetActive(true);
				WeedGrowthRateB.MasterNetSetActive(true);
				ReagentInjectobject.MasterNetSetActive(true);
				AbilityInjectobject.MasterNetSetActive(true);
			}
			else
			{
				PotencyB.MasterNetSetActive(false);
				YieldB.MasterNetSetActive(false);
				ProductionSpeedB.MasterNetSetActive(false);
				enduranceB.MasterNetSetActive(false);
				LifespanB.MasterNetSetActive(false);
				WeedResistanceB.MasterNetSetActive(false);
				WeedGrowthRateB.MasterNetSetActive(false);
				ReagentInjectobject.MasterNetSetActive(false);
				AbilityInjectobject.MasterNetSetActive(false);
			}

			bool Seed = PlantDNAManipulator.SeedPacket != null;


			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.Potency != -1)
			{
				if (Seed)
				{
					PotencyB.MasterNetSetActive(true);
					PotencyBT.MasterSetValue("Inject " + PlantDNAManipulator.PlantDNADataDisc.LoadedData.Potency);
				}

				CartridgeText.MasterSetValue("Potency " + PlantDNAManipulator.PlantDNADataDisc.LoadedData.Potency);
			}
			else if (Seed)
			{
				PotencyBT.MasterSetValue("Extract");
			}


			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.Yield != -1)
			{
				if (Seed)
				{
					YieldB.MasterNetSetActive(true);
					YieldBT.MasterSetValue("Inject " + PlantDNAManipulator.PlantDNADataDisc.LoadedData.Yield);
				}

				CartridgeText.MasterSetValue("Yield " + PlantDNAManipulator.PlantDNADataDisc.LoadedData.Yield);
			}
			else if (Seed)
			{
				YieldBT.MasterSetValue("Extract");
			}


			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.GrowthSpeed != -1)
			{
				if (Seed)
				{
					ProductionSpeedB.MasterNetSetActive(true);
					ProductionSpeedBT.MasterSetValue("Inject " +
					                                 PlantDNAManipulator.PlantDNADataDisc.LoadedData.GrowthSpeed);
				}

				CartridgeText.MasterSetValue("Production Speed " +
				                             PlantDNAManipulator.PlantDNADataDisc.LoadedData.GrowthSpeed);
			}
			else if (Seed)
			{
				ProductionSpeedBT.MasterSetValue("Extract");
			}


			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.Endurance != -1)
			{
				if (Seed)
				{
					enduranceB.MasterNetSetActive(true);
					enduranceBT.MasterSetValue("Inject " + PlantDNAManipulator.PlantDNADataDisc.LoadedData.Endurance);
				}

				CartridgeText.MasterSetValue("Endurance " +
				                             PlantDNAManipulator.PlantDNADataDisc.LoadedData.Endurance);
			}
			else if (Seed)
			{
				enduranceBT.MasterSetValue("Extract");
			}

			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.Lifespan != -1)
			{
				if (Seed)
				{
					LifespanB.MasterNetSetActive(true);
					LifespanBT.MasterSetValue("Inject " + PlantDNAManipulator.PlantDNADataDisc.LoadedData.Lifespan);
				}

				CartridgeText.MasterSetValue("Lifespan " +
				                             PlantDNAManipulator.PlantDNADataDisc.LoadedData.Lifespan);
			}
			else if (Seed)
			{
				LifespanBT.MasterSetValue("Extract");
			}

			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.WeedResistance != -1)
			{
				if (Seed)
				{
					WeedResistanceB.MasterNetSetActive(true);
					WeedResistanceBT.MasterSetValue("Inject " +
					                                PlantDNAManipulator.PlantDNADataDisc.LoadedData.WeedResistance);
				}

				CartridgeText.MasterSetValue("Weed Resistance " +
				                             PlantDNAManipulator.PlantDNADataDisc.LoadedData.WeedResistance);
			}
			else if (Seed)
			{
				WeedResistanceBT.MasterSetValue("Extract");
			}

			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.WeedGrowthRate != -1)
			{
				if (Seed)
				{
					WeedGrowthRateB.MasterNetSetActive(true);
					WeedGrowthRateBT.MasterSetValue("Inject " +
					                                PlantDNAManipulator.PlantDNADataDisc.LoadedData.WeedGrowthRate);
				}

				CartridgeText.MasterSetValue("Weed Growth Rate " +
				                             PlantDNAManipulator.PlantDNADataDisc.LoadedData.WeedGrowthRate);
			}
			else if (Seed)
			{
				WeedGrowthRateBT.MasterSetValue("Extract");
			}


			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.ReagentProduction.Count > 0)
			{
				var Reagent = PlantDNAManipulator.PlantDNADataDisc.LoadedData.ReagentProduction[0];
				if (Seed)
				{

					ReagentInjectobject.MasterNetSetActive(true);
					ReagentInjectText.MasterSetValue("Inject " + Reagent.ChemistryReagent.name + " " +
					                                 Reagent.percentage);
				}

				CartridgeText.MasterSetValue("Produce " + Reagent.ChemistryReagent.name + " " + Reagent.percentage);
			}
			else if (Seed)
			{
				ReagentInjectobject.MasterNetSetActive(false);
				ReagentInjectText.MasterSetValue("Extract");
			}

			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.PlantTrays.Count > 0)
			{
				var Trey = PlantDNAManipulator.PlantDNADataDisc.LoadedData.PlantTrays[0];
				if (Seed)
				{
					AbilityInjectobject.MasterNetSetActive(true);
					AbilityInjectText.MasterSetValue("Inject " + Trey);
				}

				CartridgeText.MasterSetValue("Trait  " + Trey);
			}
			else if (Seed)
			{
				AbilityInjectobject.MasterNetSetActive(false);
				AbilityInjectText.MasterSetValue("Extract");
			}
		}
		else
		{
			CartridgeText.MasterSetValue("None");
			PotencyB.MasterNetSetActive(false);
			YieldB.MasterNetSetActive(false);
			ProductionSpeedB.MasterNetSetActive(false);
			enduranceB.MasterNetSetActive(false);
			LifespanB.MasterNetSetActive(false);
			WeedResistanceB.MasterNetSetActive(false);
			WeedGrowthRateB.MasterNetSetActive(false);
			ReagentInjectobject.MasterNetSetActive(false);
			AbilityInjectobject.MasterNetSetActive(false);
		}

		if (PlantDNAManipulator.SeedPacket == null)
		{
			Potency.MasterSetValue("N/A");
			Yield.MasterSetValue("N/A");
			ProductionSpeed.MasterSetValue("N/A");
			endurance.MasterSetValue("N/A");
			Lifespan.MasterSetValue("N/A");
			WeedResistance.MasterSetValue("N/A");
			WeedGrowthRate.MasterSetValue("N/A");
			SeedText.MasterSetValue("None");
		}
		else
		{
			SeedText.MasterSetValue(PlantDNAManipulator.SeedPacket.gameObject.ExpensiveName());
			Potency.MasterSetValue(PlantDNAManipulator.SeedPacket.plantData.Potency.ToString());
			Yield.MasterSetValue(PlantDNAManipulator.SeedPacket.plantData.Yield.ToString());
			ProductionSpeed.MasterSetValue(PlantDNAManipulator.SeedPacket.plantData.GrowthSpeed.ToString());
			endurance.MasterSetValue(PlantDNAManipulator.SeedPacket.plantData.Endurance.ToString());
			Lifespan.MasterSetValue(PlantDNAManipulator.SeedPacket.plantData.Lifespan.ToString());
			WeedResistance.MasterSetValue(PlantDNAManipulator.SeedPacket.plantData.WeedResistance.ToString());
			WeedGrowthRate.MasterSetValue(PlantDNAManipulator.SeedPacket.plantData.WeedGrowthRate.ToString());

			foreach (var Reagent in PlantDNAManipulator.SeedPacket.plantData.ReagentProduction)
			{
				ReagentsSynchronousList.AddElementReagent(Reagent, ShowExtract, this);
			}

			foreach (var PlantTray in PlantDNAManipulator.SeedPacket.plantData.PlantTrays)
			{
				AbilitysSynchronousList.AddElementSetupAbility(PlantTray, ShowExtract, this);
			}
		}
	}


	public void EjectSeed(PlayerInfo subject)
	{
		if (PlantDNAManipulator.SeedPacket == null) return;
		if (Inventory.ServerTransfer(PlantDNAManipulator.ItemStorage.GetIndexedItemSlot(1),
			    subject.Script.DynamicItemStorage.GetBestHandOrSlotFor(PlantDNAManipulator.SeedPacket.gameObject)) ==
		    false)
		{
			Inventory.ServerDrop(PlantDNAManipulator.ItemStorage.GetIndexedItemSlot(1));
		}

		UpdateDisplay();
	}

	public void EjectCartridge(PlayerInfo subject)
	{
		if (PlantDNAManipulator.PlantDNADataDisc == null) return;
		if (Inventory.ServerTransfer(PlantDNAManipulator.ItemStorage.GetIndexedItemSlot(0),
			    subject.Script.DynamicItemStorage.GetBestHandOrSlotFor(PlantDNAManipulator.PlantDNADataDisc
				    .gameObject)) == false)
		{
			Inventory.ServerDrop(PlantDNAManipulator.ItemStorage.GetIndexedItemSlot(0));
		}

		UpdateDisplay();
	}

	public void InjectReagent()
	{
		if (PlantDNAManipulator.SeedPacket == null) return;
		if (PlantDNAManipulator.PlantDNADataDisc == null) return;

		if (PlantDNAManipulator.PlantDNADataDisc.IsEmpty())
		{
			return;
		}
		else
		{
			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.ReagentProduction.Count > 0)
			{
				var InjectingReagent = PlantDNAManipulator.PlantDNADataDisc.LoadedData.ReagentProduction[0];
				if (PlantDNAManipulator.SeedPacket.plantData.ReagentProduction.Any(x =>
					    x.ChemistryReagent == InjectingReagent.ChemistryReagent))
				{
					return;
				}
				else
				{
					PlantDNAManipulator.SeedPacket.plantData.ReagentProduction.Add(InjectingReagent);
				}
			}
			else if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.PlantTrays.Count > 0)
			{
				var InjectingPlantTrays = PlantDNAManipulator.PlantDNADataDisc.LoadedData.PlantTrays[0];
				if (PlantDNAManipulator.SeedPacket.plantData.PlantTrays.Contains(InjectingPlantTrays))
				{
					return;
				}
				else
				{
					PlantDNAManipulator.SeedPacket.plantData.PlantTrays.Add(InjectingPlantTrays);
				}
			}
		}

		UpdateDisplay();
	}


	public void InteractPotency()
	{
		if (PlantDNAManipulator.SeedPacket == null) return;
		if (PlantDNAManipulator.PlantDNADataDisc == null) return;

		if (PlantDNAManipulator.PlantDNADataDisc.IsEmpty())
		{
			PlantDNAManipulator.PlantDNADataDisc.LoadedData.Potency = PlantDNAManipulator.SeedPacket.plantData.Potency;
			_ = Despawn.ServerSingle(PlantDNAManipulator.SeedPacket.gameObject);
		}
		else
		{
			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.Potency != -1)
			{
				PlantDNAManipulator.SeedPacket.plantData.Potency =
					PlantDNAManipulator.PlantDNADataDisc.LoadedData.Potency;
			}
		}

		UpdateDisplay();
	}

	public void InteractYield()
	{
		if (PlantDNAManipulator.SeedPacket == null) return;
		if (PlantDNAManipulator.PlantDNADataDisc == null) return;

		if (PlantDNAManipulator.PlantDNADataDisc.IsEmpty())
		{
			PlantDNAManipulator.PlantDNADataDisc.LoadedData.Yield = PlantDNAManipulator.SeedPacket.plantData.Yield;
			_ = Despawn.ServerSingle(PlantDNAManipulator.SeedPacket.gameObject);
		}
		else
		{
			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.Yield != -1)
			{
				PlantDNAManipulator.SeedPacket.plantData.Yield = PlantDNAManipulator.PlantDNADataDisc.LoadedData.Yield;
			}
		}

		UpdateDisplay();
	}

	public void InteractProductionSpeed()
	{
		if (PlantDNAManipulator.SeedPacket == null) return;
		if (PlantDNAManipulator.PlantDNADataDisc == null) return;

		if (PlantDNAManipulator.PlantDNADataDisc.IsEmpty())
		{
			PlantDNAManipulator.PlantDNADataDisc.LoadedData.GrowthSpeed =
				PlantDNAManipulator.SeedPacket.plantData.GrowthSpeed;
			_ = Despawn.ServerSingle(PlantDNAManipulator.SeedPacket.gameObject);
		}
		else
		{
			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.GrowthSpeed != -1)
			{
				PlantDNAManipulator.SeedPacket.plantData.GrowthSpeed =
					PlantDNAManipulator.PlantDNADataDisc.LoadedData.GrowthSpeed;
			}
		}

		UpdateDisplay();
	}

	public void InteractEndurance()
	{
		if (PlantDNAManipulator.SeedPacket == null) return;
		if (PlantDNAManipulator.PlantDNADataDisc == null) return;

		if (PlantDNAManipulator.PlantDNADataDisc.IsEmpty())
		{
			PlantDNAManipulator.PlantDNADataDisc.LoadedData.Endurance =
				PlantDNAManipulator.SeedPacket.plantData.Endurance;
			_ = Despawn.ServerSingle(PlantDNAManipulator.SeedPacket.gameObject);
		}
		else
		{
			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.Endurance != -1)
			{
				PlantDNAManipulator.SeedPacket.plantData.Endurance =
					PlantDNAManipulator.PlantDNADataDisc.LoadedData.Endurance;
			}
		}

		UpdateDisplay();
	}

	public void InteractLifespan()
	{
		if (PlantDNAManipulator.SeedPacket == null) return;
		if (PlantDNAManipulator.PlantDNADataDisc == null) return;

		if (PlantDNAManipulator.PlantDNADataDisc.IsEmpty())
		{
			PlantDNAManipulator.PlantDNADataDisc.LoadedData.Lifespan =
				PlantDNAManipulator.SeedPacket.plantData.Lifespan;
			_ = Despawn.ServerSingle(PlantDNAManipulator.SeedPacket.gameObject);
		}
		else
		{
			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.Lifespan != -1)
			{
				PlantDNAManipulator.SeedPacket.plantData.Lifespan =
					PlantDNAManipulator.PlantDNADataDisc.LoadedData.Lifespan;
			}
		}

		UpdateDisplay();
	}

	public void InteractWeedResistance()
	{
		if (PlantDNAManipulator.SeedPacket == null) return;
		if (PlantDNAManipulator.PlantDNADataDisc == null) return;

		if (PlantDNAManipulator.PlantDNADataDisc.IsEmpty())
		{
			PlantDNAManipulator.PlantDNADataDisc.LoadedData.WeedResistance =
				PlantDNAManipulator.SeedPacket.plantData.WeedResistance;
			_ = Despawn.ServerSingle(PlantDNAManipulator.SeedPacket.gameObject);
		}
		else
		{
			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.WeedResistance != -1)
			{
				PlantDNAManipulator.SeedPacket.plantData.WeedResistance =
					PlantDNAManipulator.PlantDNADataDisc.LoadedData.WeedResistance;
			}
		}

		UpdateDisplay();
	}


	public void InteractWeedGrowthRate()
	{
		if (PlantDNAManipulator.SeedPacket == null) return;
		if (PlantDNAManipulator.PlantDNADataDisc == null) return;


		if (PlantDNAManipulator.PlantDNADataDisc.IsEmpty())
		{
			PlantDNAManipulator.PlantDNADataDisc.LoadedData.WeedGrowthRate =
				PlantDNAManipulator.SeedPacket.plantData.WeedGrowthRate;
			_ = Despawn.ServerSingle(PlantDNAManipulator.SeedPacket.gameObject);
		}
		else
		{
			if (PlantDNAManipulator.PlantDNADataDisc.LoadedData.WeedGrowthRate != -1)
			{
				PlantDNAManipulator.SeedPacket.plantData.WeedGrowthRate =
					PlantDNAManipulator.PlantDNADataDisc.LoadedData.WeedGrowthRate;
			}
		}

		UpdateDisplay();
	}
}