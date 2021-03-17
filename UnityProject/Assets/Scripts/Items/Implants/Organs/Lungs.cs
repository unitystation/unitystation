using System;
using System.Collections.Generic;
using Systems.Atmospherics;
using Chemistry;
using HealthV2;
using Objects.Atmospherics;
using UnityEngine;
using Random = UnityEngine.Random;

public class Lungs : BodyPart
{
	/// <summary>
	/// The number of ticks to wait until next breath is attempted
	/// </summary>
	[Tooltip("The number of ticks to wait until next breath is attempted")]
	[SerializeField] private int breatheCooldown = 4;

	/// <summary>
	/// The minimum presure of the required gas needed to avoid suffocation
	/// </summary>
	[Tooltip("The minimum presure of the required gas needed to avoid suffocation")]
	[SerializeField] private float reagentSafeMin = 16;

	/// <summary>
	/// The gas that this tries to put into the blood stream
	/// </summary>
	[Tooltip("The gas that this tries to put into the blood stream")]
	[SerializeField] private Gas requiredGas = Gas.Oxygen;

	/// <summary>
	/// The gas that this expels when breathing out
	/// </summary>
	[Tooltip("The gas that this expels when breathing out")]
	[SerializeField] private Gas expelledGas = Gas.CarbonDioxide;

	/// <summary>
	/// The base amount of blood that this attempts to process each single breath
	/// </summary>
	[Tooltip("The base amount of blood in litres that this processes each breath")]
	public float LungProcessAmount = 10;

	/// <summary>
	/// The type of blood that this works with
	/// </summary>
	[Tooltip("The type of blood that this works with")]
	public BloodType InteractsWith;

	public override void ImplantPeriodicUpdate()
	{
		base.ImplantPeriodicUpdate();

		Vector3Int position = healthMaster.ObjectBehaviour.AssumedWorldPositionServer();
		MetaDataNode node = MatrixManager.GetMetaDataAt(position);

		if (TryBreathing(node))
		{
			AtmosManager.Update(node);
		}
	}

	/// <summary>
	/// Performs the action of breathing, expelling waste products from the used blood pool and refreshing
	/// the desired blood reagent (ie oxygen)
	/// </summary>
	/// <param name="node">The gas node at this lung's position</param>
	/// <returns>True if gas was exchanged</returns>

	// TODO: May want to have a check for the circulatory system having the same desired gas as these lungs
	private bool TryBreathing(IGasMixContainer node)
	{
		//Breathing is not timebased, but tick based, it will be slow when the blood has all the oxygen it needs
		//and will speed up if more oxygen is needed
		breatheCooldown--;
		if (breatheCooldown > 0)
		{
			return false;
		}
		
		if (healthMaster.CirculatorySystem.UsedBloodPool.Total == 0)
		{
			//No point breathing if we dont have blood.
			return false;
		}

		// Try to get internal breathing if possible, otherwise get from the surroundings
		IGasMixContainer container = healthMaster.RespiratorySystem.GetInternalGasMix() ?? node;

		//Can probably edit this to use the volume of the lungs instead.
		GasMix gasMix = container.GasMix;

		var AvailableBlood = healthMaster.CirculatorySystem.UsedBloodPool.Take(LungProcessAmount * TotalModified);
		bool tryExhale = BreatheOut(gasMix, AvailableBlood);
		bool tryInhale = BreatheIn(gasMix, AvailableBlood);

		healthMaster.CirculatorySystem.AddUsefulBloodReagent(AvailableBlood);

		return tryExhale || tryInhale;
	}

	/// <summary>
	/// Expels unwanted gases from the blood stream into the given gas mix
	/// </summary>
	/// <param name="gasMix">The gas mix to breathe out into</param>
	/// <param name="blood">The blood to pull gases from</param>
	/// <returns>True if gas was exhaled</returns>
	private bool BreatheOut(GasMix gasMix, ReagentMix blood)
	{
		ReagentMix toExhale = new ReagentMix();
		foreach (var Reagent in blood)
		{
			if (GAS2ReagentSingleton.Instance.DictionaryReagentToGas.ContainsKey(Reagent.Key))
			{
				// Prevent lungs removing desired gases and non gases from blood. 
				// May want to add other gases that the lungs are unable to remove as well (ie toxins)
				var gas = GAS2ReagentSingleton.Instance.GetReagentToGas(Reagent.Key);
				if (gas != requiredGas && Reagent.Value > 0)
				{
					toExhale.Add(Reagent.Key, Reagent.Value);
				}
			}
		}
		healthMaster.RespiratorySystem.GasExchange(gasMix, blood, toExhale, true);
		return toExhale.Total > 0;
	}

	/// <summary>
	/// Pulls in the desired gas, as well as others, from the specified gas mix and adds them to the blood stream
	/// </summary>
	/// <param name="gasMix">The gas mix to breathe in from</param>
	/// <param name="blood">The blood to put gases into</param>
	/// <returns> True if gas was inhaled </returns>
	private bool BreatheIn(GasMix gasMix, ReagentMix blood)
	{
		//Fill lungs
		float reagentInhaled = healthMaster.RespiratorySystem.HandleBreathing(gasMix, requiredGas, reagentSafeMin) * 10000f;
		
		if (reagentInhaled >= InteractsWith.GetSpareCapacity(blood))
		{
			// Lungs are able to bring blood to capacity, only pull in at most as much reagent as the blood can carry
			reagentInhaled = InteractsWith.GetSpareCapacity(blood);
			breatheCooldown = 4; //Slow breathing, we're all good
		}

		ReagentMix toInhale = new ReagentMix();
		if (GAS2ReagentSingleton.Instance.DictionaryGasToReagent.ContainsKey(requiredGas))
		{
			toInhale.Add(GAS2ReagentSingleton.Instance.GetGasToReagent(requiredGas), reagentInhaled);
		}
		else
		{
			Logger.Log("Lung's requiredGas is set to something that is not a gas!", Category.Health);
		}

		//Whenever desired gases are added, some undesired gases may be added as well
		float BloodGasCapability = blood[requiredReagent] * 0.01f;
		float TotalGas = gasMix.Moles;
		for (int i = 0; i < gasMix.Gases.Length; i++)
		{
			if (GAS2ReagentSingleton.Instance.DictionaryGasToReagent.ContainsKey(Gas.All[i]))
			{
				float quantity = gasMix.Gases[i] / TotalGas * BloodGasCapability;
				toInhale.Add(GAS2ReagentSingleton.Instance.GetGasToReagent(Gas.All[i]), quantity);
			}
		}
		healthMaster.RespiratorySystem.GasExchange(gasMix, blood, toInhale);

		// Assuming oxygen, 1 CO2 was produced for every 1 O2 consumed, so add that to what was breathed out
		// TODO: May want to have body parts add the expelled gas to the blood pool rather than doing it here
		gasMix.AddGas(expelledGas, reagentInhaled / 10000f);
		return toInhale.Total > 0;
	}
}