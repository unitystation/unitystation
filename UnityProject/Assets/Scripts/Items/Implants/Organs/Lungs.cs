using System;
using System.Collections.Generic;
using Systems.Atmospherics;
using Chemistry;
using HealthV2;
using Objects.Atmospherics;
using UnityEngine;
using Random = UnityEngine.Random;

public class Lungs : BodyPartModification
{
	/// <summary>
	/// The number of ticks to wait until next breath is attempted
	/// </summary>
	[Tooltip("The number of ticks to wait until next breath is attempted")]
	[SerializeField] private int breatheCooldown = 4;
	private int currentBreatheCooldown = 4;

	/// <summary>
	/// The minimum pressure of the required gas needed to avoid suffocation
	/// </summary>
	[Tooltip("The minimum pressure needed to avoid suffocation")]
	[SerializeField] private float pressureSafeMin = 16;

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
	//[Tooltip("The base amount of blood in litres that this processes each breath")]
	//public float LungProcessAmount = 1.5f;

	/// <summary>
	/// The volume of the lung in litres
	/// </summary>
	[Tooltip("The volume of the lung in litres")]
	public float LungSize = 6;

	[SerializeField, Range(0,100)] private float coughChanceWhenInternallyBleeding = 32;
	[SerializeField] private float internalBleedingCooldown = 4f;
    private bool onCooldown = false;

	public override void ImplantPeriodicUpdate()
	{
		base.ImplantPeriodicUpdate();

		Vector3Int position = RelatedPart.HealthMaster.ObjectBehaviour.AssumedWorldPositionServer();
		MetaDataNode node = MatrixManager.GetMetaDataAt(position);

		if (TryBreathing(node, RelatedPart.TotalModified))
		{
			AtmosManager.Update(node);
		}

		if(RelatedPart.IsBleedingInternally)
		{
			InternalDamageLogic();
		}
	}

	/// <summary>
	/// Performs the action of breathing, expelling waste products from the used blood pool and refreshing
	/// the desired blood reagent (ie oxygen)
	/// </summary>
	/// <param name="node">The gas node at this lung's position</param>
	/// <returns>True if gas was exchanged</returns>
	public bool TryBreathing(IGasMixContainer node, float efficiency)
	{
		//Base effeciency is a little strong on the lungs
		//efficiency = (1 + efficiency) / 2;

		//Breathing is not timebased, but tick based, it will be slow when the blood has all the oxygen it needs
		//and will speed up if more oxygen is needed
		currentBreatheCooldown--;
		if (currentBreatheCooldown > 0)
		{
			return false;
		}

		if (RelatedPart.HealthMaster.CirculatorySystem.UsedBloodPool[RelatedPart.bloodType] == 0)
		{
			return false; //No point breathing if we dont have blood.
		}

		// Try to get internal breathing if possible, otherwise get from the surroundings
		IGasMixContainer container = RelatedPart.HealthMaster.RespiratorySystem.GetInternalGasMix() ?? node;

		ReagentMix AvailableBlood = RelatedPart.HealthMaster.CirculatorySystem.UsedBloodPool.Take(RelatedPart.HealthMaster.CirculatorySystem.UsedBloodPool.Total);

		bool tryExhale = BreatheOut(container.GasMix, AvailableBlood, efficiency);
		bool tryInhale = BreatheIn(container.GasMix, AvailableBlood, efficiency);
		RelatedPart.HealthMaster.CirculatorySystem.ReadyBloodPool.Add(AvailableBlood);

		return tryExhale || tryInhale;
	}

	/// <summary>
	/// Expels unwanted gases from the blood stream into the given gas mix
	/// </summary>
	/// <param name="gasMix">The gas mix to breathe out into</param>
	/// <param name="blood">The blood to pull gases from</param>
	/// <returns> True if breathGasMix was changed </returns>
	private bool BreatheOut(GasMix gasMix, ReagentMix blood, float efficiency)
	{
		// This isn't exactly realistic, should also factor concentration of gases in the gasMix
		ReagentMix toExhale = new ReagentMix();
		foreach (var Reagent in blood.reagents.m_dict)
		{
			if (GAS2ReagentSingleton.Instance.DictionaryReagentToGas.ContainsKey(Reagent.Key))
			{
				// Try to prevent lungs removing desired gases and non gases from blood.
				// May want to add other gases that the lungs are unable to remove as well (ie toxins)
				var gas = GAS2ReagentSingleton.Instance.GetReagentToGas(Reagent.Key);
				if (gas != requiredGas && Reagent.Value > 0)
				{
					toExhale.Add(Reagent.Key, Reagent.Value * efficiency);
				}
				else if (gas == requiredGas && Reagent.Value > 0)
				{
					float ratio;
					if (gasMix.GetPressure(requiredGas) < pressureSafeMin)
					{
						ratio = 1 - gasMix.GetPressure(requiredGas) / pressureSafeMin;
					}
					else
					{
						// Will still lose required gas suspended in blood plasma
						ratio = RelatedPart.bloodType.BloodGasCapability / RelatedPart.bloodType.BloodCapacityOf;
					}
					toExhale.Add(Reagent.Key, ratio * Reagent.Value * efficiency);
				}
			}
		}
		RelatedPart.HealthMaster.RespiratorySystem.GasExchangeFromBlood(gasMix, blood, toExhale);
		//Debug.Log("Gas exhaled: " + toExhale.Total);
		return toExhale.Total > 0;
	}

	/// <summary>
	/// Pulls in the desired gas, as well as others, from the specified gas mix and adds them to the blood stream
	/// </summary>
	/// <param name="gasMix">The gas mix to breathe in from</param>
	/// <param name="blood">The blood to put gases into</param>
	/// <returns> True if breathGasMix was changed </returns>
	private bool BreatheIn(GasMix breathGasMix, ReagentMix blood, float efficiency)
	{
		if (RelatedPart.HealthMaster.RespiratorySystem.CanBreathAnywhere)
		{
			blood.Add(RelatedPart.requiredReagent, RelatedPart.bloodType.GetSpareGasCapacity(blood));
			return false;
		}
		ReagentMix toInhale = new ReagentMix();

		for (int i = 0; i < breathGasMix.Gases.Length; i++)
		{
			if (GAS2ReagentSingleton.Instance.DictionaryGasToReagent.ContainsKey(Gas.All[i]))
			{
				// n = PV/RT
				float gasMoles = breathGasMix.GetPressure(Gas.All[i]) * LungSize / 8.314f / breathGasMix.Temperature;

				// Get as much as we need, or as much as in the lungs, whichever is lower
				Reagent gasReagent = GAS2ReagentSingleton.Instance.GetGasToReagent(Gas.All[i]);
				float molesRecieved = Mathf.Min(gasMoles, RelatedPart.bloodType.GetSpareGasCapacity(blood, gasReagent));
				toInhale.Add(gasReagent, molesRecieved * efficiency);

				//TODO: Add pressureSafeMax check here, for hyperoxia
			}
		}
		RelatedPart.HealthMaster.RespiratorySystem.GasExchangeToBlood(breathGasMix, blood, toInhale);

		// Counterintuitively, in humans respiration is stimulated by pressence of CO2 in the blood, not lack of oxygen
		// May want to change this code to reflect that in the future so people don't hyperventilate when they are on nitrous oxide
		var inGas = GAS2ReagentSingleton.Instance.GetGasToReagent(requiredGas);
		var saturation = (toInhale[inGas] + blood[inGas]) / RelatedPart.bloodType.GetGasCapacity(blood);
		if (saturation >=RelatedPart.HealthMaster.CirculatorySystem.BloodInfo.BLOOD_REAGENT_SATURATION_OKAY)
		{
			currentBreatheCooldown = breatheCooldown; //Slow breathing, we're all good
			RelatedPart.HealthMaster.HealthStateController.SetSuffocating(false);
		}
		else if (saturation <= RelatedPart.HealthMaster.CirculatorySystem.BloodInfo.BLOOD_REAGENT_SATURATION_BAD)
		{
			RelatedPart.HealthMaster.HealthStateController.SetSuffocating(true);
			if (Random.value < 0.2)
			{
				Chat.AddActionMsgToChat(RelatedPart.HealthMaster.gameObject, "You gasp for breath", $"{RelatedPart.HealthMaster.PlayerScriptOwner.visibleName} gasps");
			}
		}

		//Debug.Log("Gas inhaled: " + toInhale.Total + " Saturation: " + saturation);
		return toInhale.Total > 0;
	}

	public override void InternalDamageLogic()
	{
        if(!onCooldown)
        {
			if(RelatedPart.CurrentInternalBleedingDamage > RelatedPart.MaximumInternalBleedDamage / 2)
			{
				Chat.AddActionMsgToChat(RelatedPart.HealthMaster.gameObject, 
				"You gasp for air; but you drown in your own blood from the inside!", 
				$"{RelatedPart.HealthMaster.PlayerScriptOwner.visibleName} gasps for air!");
				RelatedPart.HealthMaster.HealthStateController.SetSuffocating(true);
			}
			else
			{
				RelatedPart.InternalBleedingLogic();
			}
            if(DMMath.Prob(coughChanceWhenInternallyBleeding))
            {
				Chat.AddActionMsgToChat(RelatedPart.HealthMaster.gameObject, 
				"You cough up blood!", $"{RelatedPart.HealthMaster.PlayerScriptOwner.visibleName} coughs up blood!");
				float bloodToCoughUp = RelatedPart.InternalBleedingBloodLoss;
				if(RelatedPart.Severity > DamageSeverity.Bad)
				{
					bloodToCoughUp += 10;
				}
				RelatedPart.CurrentInternalBleedingDamage -= bloodToCoughUp;
				EffectsFactory.BloodSplat(RelatedPart.HealthMaster.gameObject.RegisterTile().WorldPositionServer, BloodSplatSize.small, BloodSplatType.red);
            }
			onCooldown = true;
            StartCoroutine(CooldownTick());
        }
	}

    private IEnumerator<WaitForSeconds> CooldownTick()
    {
        yield return new WaitForSeconds(internalBleedingCooldown);
        onCooldown = false;
    }
}