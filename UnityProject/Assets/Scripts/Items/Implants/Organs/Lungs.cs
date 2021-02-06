using System;
using Systems.Atmospherics;
using HealthV2;
using Objects.Atmospherics;
using UnityEngine;
using Random = UnityEngine.Random;

public class Lungs : BodyPart
{
	[SerializeField] private int breatheCooldown = 4;

	[SerializeField] private float reagentSafeMin = 16;

	[SerializeField] private Gas requiredGas = Gas.Oxygen;

	[SerializeField] private Gas expelledGas = Gas.CarbonDioxide;

	private bool isSuffocating = false;
	public bool IsSuffocating => isSuffocating;


	public float LungProcessAmount = 10;

	public BloodType InteractsWith;

	public override void ImplantPeriodicUpdate(LivingHealthMasterBase healthMaster)
	{
		base.ImplantPeriodicUpdate(healthMaster);

		Vector3Int position = healthMaster.OBehavior.AssumedWorldPositionServer();
		MetaDataNode node = MatrixManager.GetMetaDataAt(position);

		if (Breathe(node, healthMaster))
		{
			AtmosManager.Update(node);
		}
	}


	private bool Breathe(IGasMixContainer node, LivingHealthMasterBase healthMaster)
	{
		// Logger.Log("Lungs have " + healthMaster.CirculatorySystem.UseBloodPool + " Of Used blood available ");
		if (healthMaster.CirculatorySystem.UseBloodPool.Total == 0) //No point breathing if we dont have blood.
		{
			return false;
		}

		//TODO: This should also make sure that the circulatory system accepts the type of gas these lungs do!

		// if no internal breathing is possible, get the from the surroundings
		IGasMixContainer container = node;
		if (healthMaster is PlayerHealthV2 playerHealth)
		{
			container = GetInternalGasMix(playerHealth) ?? node;
		}


		//Can probably edit this to use the volume of the lungs instead.
		GasMix gasMix = container.GasMix;


		var AvailableBlood = healthMaster.CirculatorySystem.UseBloodPool.Take(LungProcessAmount * TotalModified);
		float reagentUsed = HandleBreathing(gasMix);
		float gasUsed = reagentUsed;
		reagentUsed = reagentUsed * 10000f;


		if (reagentUsed > InteractsWith.GetSpareCapacity(AvailableBlood))
		{
			//Calculate it better
			reagentUsed = InteractsWith.GetSpareCapacity(AvailableBlood);
			gasUsed = reagentUsed / 10000f;
		}
		else
		{
			// var Bloodremove = (AvailableBlood[requiredReagent] - reagentUsed);
			// AvailableBlood.Remove(requiredReagent, Bloodremove);
			// healthMaster.CirculatorySystem.UseBloodPool.Add(requiredReagent, Bloodremove);
		}

		AvailableBlood.Add(requiredReagent, reagentUsed);


		// Logger.Log("Lungs produced " + reagentUsed + " Of useful blood");


		gasMix.RemoveGas(Gas.Oxygen, gasUsed);
		node.GasMix.AddGas(Gas.CarbonDioxide, gasUsed);
		healthMaster.RegisterTile.Matrix.MetaDataLayer.UpdateSystemsAt(healthMaster.RegisterTile.LocalPositionClient,
			SystemType.AtmosSystem);

		healthMaster.CirculatorySystem.AddUsefulBloodReagent(AvailableBlood);


		return reagentUsed > 0;
	}

	//A bit hacky, get the gas mask the player is wearing if they have one.
	private GasContainer GetInternalGasMix(PlayerHealthV2 playerHealth)
	{
		PlayerScript playerScript = playerHealth.RegPlayer.PlayerScript;
		if (playerScript != null)
		{
			// Check if internals exist
			var maskItemAttrs = playerScript.ItemStorage.GetNamedItemSlot(NamedSlot.mask).ItemAttributes;
			bool internalsEnabled = playerHealth.Equip.IsInternalsEnabled;
			if (maskItemAttrs != null && maskItemAttrs.CanConnectToTank && internalsEnabled)
			{
				foreach (var gasSlot in playerScript.ItemStorage.GetGasSlots())
				{
					if (gasSlot.Item == null) continue;
					var gasContainer = gasSlot.Item.GetComponent<GasContainer>();
					if (gasContainer)
					{
						return gasContainer;
					}
				}
			}
		}

		return null;
	}

	private float HandleBreathing(GasMix gasMix)
	{
		float oxygenPressure = gasMix.GetPressure(Gas.Oxygen);

		float oxygenUsed = 0;

		if (oxygenPressure < reagentSafeMin)
		{
			if (Random.value < 0.1)
			{
				Chat.AddActionMsgToChat(gameObject, "You gasp for breath", $"{gameObject.ExpensiveName()} gasps");
			}

			if (oxygenPressure > 0)
			{
				float ratio = 1 - oxygenPressure / reagentSafeMin;
				//bloodSystem.OxygenDamage += 1 * ratio;
				oxygenUsed = gasMix.GetMoles(Gas.Oxygen) * ratio * AtmosConstants.BREATH_VOLUME;
			}
			else
			{
				//bloodSystem.OxygenDamage += 1;
			}

			isSuffocating = true;
		}
		else
		{
			oxygenUsed = gasMix.GetMoles(Gas.Oxygen) * AtmosConstants.BREATH_VOLUME;
			isSuffocating = false;
			//bloodSystem.OxygenDamage -= 2.5f;
			breatheCooldown = 4;
		}

		return oxygenUsed;
	}
}